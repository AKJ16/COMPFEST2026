using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class DragDrop : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask draggableLayer;

    private WeaponData data;
    private Camera cam;
    private bool isDragging;
    private bool isPlaced;
    private Vector3 offset;
    private float zDepth;

    public WeaponData Data => data;
    public int Width => data != null ? data.Width : 1;
    public int Height => data != null ? data.Height : 1;

    private void Awake()
    {
        cam = Camera.main;
    }

    public void Initialize(WeaponData weaponData)
    {
        data = weaponData;

        if (cam == null) cam = Camera.main;

        if (TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.sprite = data.icon;
            sr.sortingOrder = 50;
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
        transform.position = mouseWorldPos + offset;

        if (GridManager.Instance != null && data != null)
        {
            GridManager.Instance.UpdateHover(this, transform.position);
        }
    }

    private void EndDrag()
    {
        isDragging = false;

        if (GridManager.Instance != null && data != null)
        {
            GridManager.Instance.ClearHover();

            Vector2Int origin = GridManager.Instance.WorldToGridPosition(transform.position, Width, Height);

            WeaponInstance placedInstance = InventorySystem.Instance.PlaceFromInventory(data, origin);

            if (placedInstance != null)
            {
                transform.position = GridManager.Instance.GridToWorldPosition(origin.x, origin.y, Width, Height);
                GridManager.Instance.OccupyCells(origin.x, origin.y, Width, Height);
                isPlaced = true;
                return;
            }
        }

        Destroy(gameObject);
    }

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