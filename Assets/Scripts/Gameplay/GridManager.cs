using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public enum CellState
    {
        Empty,
        Occupied,
        Disabled
    }

    public static GridManager Instance { get; private set; }

    [Header("Grid Setup")]
    [SerializeField] private int columns = 6;
    [SerializeField] private int rows = 4;
    [SerializeField] private float cellSize = 1f;

    [Header("Grid Cell Visual")]
    [SerializeField] private GridCell cellPrefab;
    [SerializeField] private Transform cellContainer;

    [Header("Disabled Slots")]
    [Tooltip("Add a disabled cell on the list. Disabled cells wont be spawned and left as a void")]
    [SerializeField] private List<Vector2Int> initialDisabledCells = new List<Vector2Int>();

    [Header("Gizmo Visualization Colors")]
    [SerializeField] private Color gridColor = Color.cyan;
    [SerializeField] private Color occupiedColor = Color.red;
    [SerializeField] private Color disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    private CellState[,] grid;
    private GridCell[,] cellViews; 
    private Vector3 gridOrigin;
    private List<Vector2Int> currentlyHoveredCoords = new List<Vector2Int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeGrid();
    }

    private void InitializeGrid()
    {
        grid = new CellState[columns, rows];
        cellViews = new GridCell[columns, rows];
        gridOrigin = transform.position;

        if (cellContainer == null) cellContainer = transform;

        // Mark initial disabled data states
        foreach (Vector2Int coord in initialDisabledCells)
        {
            if (IsValidCell(coord.x, coord.y))
            {
                grid[coord.x, coord.y] = CellState.Disabled;
            }
        }

        // Instantiate visual cell prefabs ONLY for valid/enabled cells
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // SKIP SPAWNING IF DISABLED (leaves an empty void)
                if (grid[x, y] == CellState.Disabled)
                {
                    cellViews[x, y] = null;
                    continue;
                }

                grid[x, y] = CellState.Empty;

                Vector3 cellPos = GridToWorldPosition(x, y, 1, 1);
                GridCell cellInstance = Instantiate(cellPrefab, cellPos, Quaternion.identity, cellContainer);
                cellInstance.gameObject.name = $"Cell_{x}_{y}";
                cellInstance.Setup(CellState.Empty);

                cellViews[x, y] = cellInstance;
            }
        }
    }

    public bool CanPlaceItem(int gridX, int gridY, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int targetX = gridX + x;
                int targetY = gridY + y;

                if (!IsValidCell(targetX, targetY)) return false;

                // Must be Empty (NOT Occupied and NOT Disabled)
                if (grid[targetX, targetY] != CellState.Empty) return false;
            }
        }
        return true;
    }

    public bool TryPlaceItem(DragDrop item, Vector3 itemWorldPos, out Vector3 snappedWorldPos)
    {
        snappedWorldPos = Vector3.zero;

        Vector2Int gridCoord = WorldToGridPosition(itemWorldPos, item.Width, item.Height);

        if (CanPlaceItem(gridCoord.x, gridCoord.y, item.Width, item.Height))
        {
            for (int x = 0; x < item.Width; x++)
            {
                for (int y = 0; y < item.Height; y++)
                {
                    int targetX = gridCoord.x + x;
                    int targetY = gridCoord.y + y;

                    grid[targetX, targetY] = CellState.Occupied;

                    if (cellViews[targetX, targetY] != null)
                    {
                        cellViews[targetX, targetY].SetState(CellState.Occupied);
                    }
                }
            }

            snappedWorldPos = GridToWorldPosition(gridCoord.x, gridCoord.y, item.Width, item.Height);
            return true;
        }

        return false;
    }

    #region Hover System

    public void UpdateHover(DragDrop item, Vector3 worldPos)
    {
        ClearHover();

        Vector2Int gridCoord = WorldToGridPosition(worldPos, item.Width, item.Height);
        bool isValidPlacement = CanPlaceItem(gridCoord.x, gridCoord.y, item.Width, item.Height);

        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                int targetX = gridCoord.x + x;
                int targetY = gridCoord.y + y;

                if (IsValidCell(targetX, targetY))
                {
                    // If a cell view exists at this location, trigger hover highlight
                    if (cellViews[targetX, targetY] != null)
                    {
                        cellViews[targetX, targetY].SetHover(true, isValidPlacement);
                        currentlyHoveredCoords.Add(new Vector2Int(targetX, targetY));
                    }
                }
            }
        }
    }

    public void ClearHover()
    {
        foreach (var coord in currentlyHoveredCoords)
        {
            if (IsValidCell(coord.x, coord.y) && cellViews[coord.x, coord.y] != null)
            {
                cellViews[coord.x, coord.y].SetHover(false);
            }
        }
        currentlyHoveredCoords.Clear();
    }

    #endregion

    #region Dynamic Cell Disabling

    public void DisableCell(int x, int y)
    {
        if (IsValidCell(x, y))
        {
            grid[x, y] = CellState.Disabled;

            // Destroy cell visual if dynamically disabled at runtime
            if (cellViews[x, y] != null)
            {
                Destroy(cellViews[x, y].gameObject);
                cellViews[x, y] = null;
            }
        }
    }

    #endregion

    #region Helpers & Coordinate Conversion

    public bool IsValidCell(int x, int y)
    {
        return x >= 0 && x < columns && y >= 0 && y < rows;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos, int width, int height)
    {
        float bottomLeftX = worldPos.x - gridOrigin.x - (width * cellSize / 2f);
        float bottomLeftY = worldPos.y - gridOrigin.y - (height * cellSize / 2f);

        int x = Mathf.RoundToInt(bottomLeftX / cellSize);
        int y = Mathf.RoundToInt(bottomLeftY / cellSize);

        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorldPosition(int gridX, int gridY, int width, int height)
    {
        float centerX = gridOrigin.x + (gridX + width / 2f) * cellSize;
        float centerY = gridOrigin.y + (gridY + height / 2f) * cellSize;

        return new Vector3(centerX, centerY, transform.position.z);
    }

    #endregion

    #region Visualization Helpers

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 cellCenter = origin + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);

                // Determine Gizmo color based on state
                if (Application.isPlaying && grid != null)
                {
                    switch (grid[x, y])
                    {
                        case CellState.Occupied:
                            Gizmos.color = occupiedColor;
                            break;
                        case CellState.Disabled:
                            Gizmos.color = disabledColor;
                            break;
                        case CellState.Empty:
                        default:
                            Gizmos.color = gridColor;
                            break;
                    }
                }
                else
                {
                    // In Editor mode, check initialDisabledCells list
                    if (initialDisabledCells.Contains(new Vector2Int(x, y)))
                    {
                        Gizmos.color = disabledColor;
                    }
                    else
                    {
                        Gizmos.color = gridColor;
                    }
                }

                Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, cellSize, 0.1f));

                // Draw X over disabled cells in gizmos for clarity
                if (Gizmos.color == disabledColor)
                {
                    float half = cellSize * 0.4f;
                    Gizmos.DrawLine(cellCenter + new Vector3(-half, -half, 0), cellCenter + new Vector3(half, half, 0));
                    Gizmos.DrawLine(cellCenter + new Vector3(-half, half, 0), cellCenter + new Vector3(half, -half, 0));
                }
            }
        }
    }

    #endregion
}