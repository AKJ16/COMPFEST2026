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

    private GridManager.CellState currentState;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(GridManager.CellState initialState)
    {
        SetState(initialState);
    }

    public void SetState(GridManager.CellState state)
    {
        currentState = state;
        RefreshColor();
    }

    public void SetHover(bool isHovered, bool isValid = true)
    {
        if (isHovered)
        {
            spriteRenderer.color = isValid ? validHoverColor : invalidHoverColor;
        }
        else
        {
            RefreshColor();
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