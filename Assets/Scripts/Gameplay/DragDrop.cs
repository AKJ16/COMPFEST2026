using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class DragDrop : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask draggableLayer;

    [Header("Juice / Drag Scaling")]
    [SerializeField] private float dragScaleMultiplier = 1.15f;
    [SerializeField] private float scaleSpeed = 16f;

    private WeaponData data;
    private Camera cam;
    private bool isDragging;
    private bool isPlaced;
    private Vector3 offset;
    private float zDepth;
    private SpriteRenderer spriteRenderer;

    private Vector3 snappedPos;
    private Coroutine animRoutine;

    public WeaponData Data => data;
    public int Width => data != null ? data.Width : 1;
    public int Height => data != null ? data.Height : 1;

    private void Awake()
    {
        cam = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(WeaponData weaponData)
    {
        data = weaponData;

        if (cam == null) cam = Camera.main;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = data.icon;
            spriteRenderer.sortingOrder = 100;
        }

        isDragging = true;
        zDepth = cam.WorldToScreenPoint(transform.position).z;

        Vector2 pointerPos = GetPointerPosition();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, zDepth));
        offset = transform.position - mouseWorldPos;
    }

    private void Update()
    {
        if (isPlaced) return;

        if (isDragging)
        {
            Vector3 targetScale = Vector3.one * dragScaleMultiplier;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
        }

        HandleInput();
    }

    private void HandleInput()
    {
        if (isDragging && IsPrimaryClickHeld())
        {
            ApplyDrag();
        }

        if (IsPrimaryClickReleased() && isDragging)
        {
            EndDrag();
        }
    }

    private void ApplyDrag()
    {
        Vector2 pointerPos = GetPointerPosition();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, zDepth));
        transform.position = new Vector3(mouseWorldPos.x + offset.x, mouseWorldPos.y + offset.y, -0.5f);

        if (GridManager.Instance != null && data != null)
        {
            GridManager.Instance.UpdateHover(this, transform.position);
        }
    }

    private void EndDrag()
    {
        isDragging = false;

        if (WeaponEffectsSystem.IsHourglassBusy)
        {
            Destroy(gameObject);
            return;
        }

        if (GridManager.Instance != null && data != null)
        {
            GridManager.Instance.ClearHover();

            Vector2Int origin = GridManager.Instance.WorldToGridPosition(transform.position, Width, Height);

            // Pass 'this' as visual reference so Hourglasses can trigger it
            WeaponInstance placedInstance = InventorySystem.Instance.PlaceFromInventory(data, origin, this);

            if (placedInstance != null)
            {
                placedInstance.VisualObject = this;

                transform.localScale = Vector3.one;
                snappedPos = GridManager.Instance.GridToWorldPosition(origin.x, origin.y, Width, Height);
                transform.position = snappedPos;

                GridManager.Instance.OccupyCells(origin.x, origin.y, Width, Height);
                isPlaced = true;

                // Play placement animation (Hourglass handles its own timing via chain reaction)
                if (data.modifierType != ModifierType.Repeat)
                {
                    PlayAnimation();
                }

                return;
            }
        }

        Destroy(gameObject);
    }

    #region Unique Weapon Animations

    public void PlayAnimation()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);

        WeaponAnimType type = data != null ? data.animType : WeaponAnimType.Auto;

        if (type == WeaponAnimType.Auto && data != null)
        {
            string name = data.weaponName.ToLower();

            // FIXED: Prioritize specific weapon names first so Swords never trigger Wand animations
            if (name.Contains("sword") || name.Contains("blade") || name.Contains("saber"))
                type = WeaponAnimType.SwordSwing;
            else if (name.Contains("dagger") || name.Contains("knife") || data.appliesPoison)
                type = WeaponAnimType.DaggerStab;
            else if (name.Contains("wand") || name.Contains("staff") || name.Contains("rod"))
                type = WeaponAnimType.WandGlow;
            else if (data.modifierType == ModifierType.Repeat || name.Contains("hourglass"))
                type = WeaponAnimType.HourglassSpin;
            else if (data.category == WeaponCategory.Modifier || name.Contains("book") || name.Contains("tome"))
                type = WeaponAnimType.BookPop;
            else
                type = WeaponAnimType.SwordSwing;
        }

        switch (type)
        {
            case WeaponAnimType.SwordSwing:
                animRoutine = StartCoroutine(SwordSwingRoutine());
                break;
            case WeaponAnimType.WandGlow:
                animRoutine = StartCoroutine(WandGlowRoutine());
                break;
            case WeaponAnimType.BookPop:
                animRoutine = StartCoroutine(BookPopRoutine());
                break;
            case WeaponAnimType.DaggerStab:
                animRoutine = StartCoroutine(DaggerStabRoutine());
                break;
            case WeaponAnimType.HourglassSpin:
                animRoutine = StartCoroutine(HourglassSpinRoutine());
                break;
        }
    }

    // 1. Sword Swing (Tilt slash down and recover)
    private IEnumerator SwordSwingRoutine(float duration = 0.28f)
    {
        float elapsed = 0f;
        transform.localScale = Vector3.one;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.Sin(t * Mathf.PI * 2f) * -35f;
            transform.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        transform.localRotation = Quaternion.identity;
    }

    // 2. Wand / Staff Glow (Grow + Bright Yellow Flash)
    private IEnumerator WandGlowRoutine(float duration = 0.35f)
    {
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;
        Color baseColor = Color.white;
        Color glowColor = new Color(1.8f, 1.8f, 1.1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ping = Mathf.Sin(t * Mathf.PI);

            transform.localScale = baseScale * (1f + ping * 0.25f);
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(baseColor, glowColor, ping);

            yield return null;
        }

        transform.localScale = baseScale;
        if (spriteRenderer != null) spriteRenderer.color = baseColor;
    }

    // 3. Book Pop (Scale punch)
    private IEnumerator BookPopRoutine(float duration = 0.22f)
    {
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ping = Mathf.Sin(t * Mathf.PI);

            transform.localScale = baseScale * (1f + ping * 0.3f);
            yield return null;
        }

        transform.localScale = baseScale;
    }

    // 4. Poison Dagger (Diagonal thrust down-right)
    private IEnumerator DaggerStabRoutine(float duration = 0.24f)
    {
        float elapsed = 0f;
        Vector3 thrustDir = new Vector3(0.28f, -0.28f, 0f);
        Vector3 originPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float offset = (t < 0.35f) ? (t / 0.35f) : (1f - ((t - 0.35f) / 0.65f));
            transform.position = originPos + thrustDir * offset;
            yield return null;
        }

        transform.position = originPos;
    }

    // 5. Hourglass Spin (Full 360-degree spin)
    private IEnumerator HourglassSpinRoutine(float duration = 0.40f)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float angle = Mathf.Lerp(0f, -360f, t);
            transform.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }

        transform.localRotation = Quaternion.identity;
    }

    #endregion

    #region Input Helpers
    private bool IsPrimaryClickHeld()
    {
        return (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
               (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);
    }

    private bool IsPrimaryClickReleased()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) ||
               (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame);
    }

    private Vector2 GetPointerPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.isInProgress)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }
    #endregion
}