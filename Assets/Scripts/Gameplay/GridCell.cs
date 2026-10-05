using UnityEngine;

public class GridCell : MonoBehaviour
{
    [Header("Sprite Reference")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Color Settings")]
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Color occupiedColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    [SerializeField] private Color validHoverColor = new Color(0.2f, 1f, 0.2f, 0.7f);
    [SerializeField] private Color invalidHoverColor = new Color(1f, 0.2f, 0.2f, 0.7f);

    [Header("Hover Scale Settings")]
    [Tooltip("How much the cell expands when hovered (e.g. 1.06 = 6% bigger to prevent ugly overlap)")]
    [SerializeField] private float hoverScaleMultiplier = 1.06f;

    private GridManager.CellState currentState;
    private Vector3 baseScale;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        baseScale = transform.localScale;
    }

    public void Setup(GridManager.CellState initialState)
    {
        baseScale = transform.localScale;
        SetState(initialState);
    }

    public void SetState(GridManager.CellState state)
    {
        currentState = state;
        RefreshColor();
        transform.localScale = baseScale;
    }

    /// <summary>
    /// Temporarily applies a subtle scale and hover color over the cell.
    /// </summary>
    public void SetHover(bool isHovered, bool isValid = true)
    {
        if (isHovered)
        {
            spriteRenderer.color = isValid ? validHoverColor : invalidHoverColor;
            transform.localScale = baseScale * hoverScaleMultiplier;
            spriteRenderer.sortingOrder = 1; // Sits neatly above neighbor borders
        }
        else
        {
            RefreshColor();
            transform.localScale = baseScale;
            spriteRenderer.sortingOrder = 0; // Returns to base grid layer
        }
    }

    private void RefreshColor()
    {
        if (currentState == GridManager.CellState.Occupied)
        {
            spriteRenderer.color = occupiedColor;
        }
        else
        {
            spriteRenderer.color = emptyColor;
        }
    }
}