using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class DragDrop : MonoBehaviour
{
    [Header("Item Grid Dimensions (Nanti ini dimasukin script weapon aja)")]
    [SerializeField] private int width = 1;
    [SerializeField] private int height = 1;

    [Header("Settings")]
    [SerializeField] private LayerMask draggableLayer;

    public int Width => width;
    public int Height => height;

    private Camera cam;
    private bool isDragging;
    private bool isPlaced;
    private Vector3 offset;
    private float zDepth;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (isPlaced || Time.timeScale == 0f) return;

        HandleInput();
    }

    private void HandleInput()
    {
        if (IsPrimaryClickedThisFrame())
        {
            StartDrag();
        }

        if (isDragging && IsPrimaryClickHeld())
        {
            ApplyDrag();
        }

        if (IsPrimaryClickReleased() && isDragging)
        {
            EndDrag();
        }
    }

    private void StartDrag()
    {
        if (IsPointerOverUI()) return;

        Vector2 pointerPos = GetPointerPosition();
        Ray ray = cam.ScreenPointToRay(pointerPos);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, draggableLayer);

        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            isDragging = true;
            zDepth = cam.WorldToScreenPoint(transform.position).z;

            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, zDepth));
            offset = transform.position - mouseWorldPos;
        }
    }

    private void ApplyDrag()
    {
        Vector2 pointerPos = GetPointerPosition();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, zDepth));
        transform.position = mouseWorldPos + offset;

        // Send continuous hover updates to GridManager while dragging
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UpdateHover(this, transform.position);
        }
    }

    private void EndDrag()
    {
        isDragging = false;

        if (GridManager.Instance != null)
        {
            // Clear hover highlights before evaluating drop
            GridManager.Instance.ClearHover();

            if (GridManager.Instance.TryPlaceItem(this, transform.position, out Vector3 snappedPos))
            {
                transform.position = snappedPos;
                LockAndAttack();
            }
        }
    }

    private void LockAndAttack()
    {
        isPlaced = true;
        Debug.Log($"{gameObject.name} placed on grid!");
    }

    #region Input Helpers (Mobile and Windows compatible)
    private bool IsPrimaryClickedThisFrame()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
               (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
    }

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

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = GetPointerPosition() };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
    #endregion
}