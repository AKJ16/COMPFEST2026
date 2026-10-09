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
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 5;
    [SerializeField] private float cellSize = 1f;

    [Header("Visual Cell Prefab")]
    [SerializeField] private GridCell cellPrefab;
    [SerializeField] private Transform cellContainer;

    [Header("Difficulty / Disabled Slots")]
    [SerializeField] private List<Vector2Int> initialDisabledCells = new List<Vector2Int>();

    [Header("Audio SFX")]
    [SerializeField] private AudioClip placeItemSfx;
    [Tooltip("Sound played whenever dragged weapons activate/hover over new grid cells.")]
    [SerializeField] private AudioClip cellHoverSfx; // Grid Cell Activation SFX

    [Header("Gizmo Visualization Colors")]
    [SerializeField] private Color gridColor = Color.cyan;
    [SerializeField] private Color occupiedColor = Color.red;
    [SerializeField] private Color disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    private CellState[,] grid;
    private GridCell[,] cellViews;
    private Vector3 gridOrigin;

    private List<Vector2Int> currentlyHoveredCoords = new List<Vector2Int>();
    private Vector2Int _lastHoverCoord = new Vector2Int(-9999, -9999);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (WeaponGridManager.Instance != null)
        {
            columns = WeaponGridManager.Instance.GridWidth;
            rows = WeaponGridManager.Instance.GridHeight;
        }

        InitializeGrid();
    }

    private void InitializeGrid()
    {
        grid = new CellState[columns, rows];
        cellViews = new GridCell[columns, rows];

        gridOrigin = transform.position - new Vector3((columns * cellSize) / 2f, (rows * cellSize) / 2f, 0f);

        if (cellContainer == null) cellContainer = transform;

        foreach (Vector2Int coord in initialDisabledCells)
        {
            if (IsValidCell(coord.x, coord.y))
            {
                grid[coord.x, coord.y] = CellState.Disabled;
            }
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
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

    public void SetGridDimensions(int newColumns, int newRows, List<Vector2Int> newDisabledCells)
    {
        DragDrop[] placedWeapons = Object.FindObjectsByType<DragDrop>();
        foreach (var weapon in placedWeapons)
        {
            Destroy(weapon.gameObject);
        }

        if (cellViews != null)
        {
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (cellViews[x, y] != null)
                    {
                        Destroy(cellViews[x, y].gameObject);
                        cellViews[x, y] = null;
                    }
                }
            }
        }

        columns = newColumns;
        rows = newRows;
        initialDisabledCells = newDisabledCells != null ? new List<Vector2Int>(newDisabledCells) : new List<Vector2Int>();

        InitializeGrid();
    }

    public void ResetVisualGrid()
    {
        DragDrop[] placedWeapons = Object.FindObjectsByType<DragDrop>();
        foreach (var weapon in placedWeapons)
        {
            Destroy(weapon.gameObject);
        }

        if (grid == null) return;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] != CellState.Disabled)
                {
                    grid[x, y] = CellState.Empty;
                    if (cellViews[x, y] != null)
                    {
                        cellViews[x, y].SetState(CellState.Empty);
                    }
                }
            }
        }
    }

    public bool IsCellEmpty(int x, int y)
    {
        if (!IsValidCell(x, y)) return false;
        return grid != null && grid[x, y] == CellState.Empty;
    }

    public bool CanPlaceItem(int gridX, int gridY, int width, int height, WeaponData data = null)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int targetX = gridX + x;
                int targetY = gridY + y;

                if (!IsValidCell(targetX, targetY)) return false;
                if (grid[targetX, targetY] != CellState.Empty) return false;
            }
        }

        if (WeaponGridManager.Instance != null && data != null)
        {
            if (!WeaponGridManager.Instance.CanPlace(data, new Vector2Int(gridX, gridY)))
                return false;
        }

        return true;
    }

    public void OccupyCells(int gridX, int gridY, int width, int height)
    {
        if (placeItemSfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(placeItemSfx);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int targetX = gridX + x;
                int targetY = gridY + y;

                if (IsValidCell(targetX, targetY))
                {
                    grid[targetX, targetY] = CellState.Occupied;

                    if (cellViews[targetX, targetY] != null)
                    {
                        cellViews[targetX, targetY].SetState(CellState.Occupied);
                    }
                }
            }
        }
    }

    #region Hover System
    public void UpdateHover(DragDrop item, Vector3 worldPos)
    {
        Vector2Int gridCoord = WorldToGridPosition(worldPos, item.Width, item.Height);

        // PLAY SOUND ONLY WHEN MOVING TO A NEW GRID COORDINATE
        bool positionChanged = (gridCoord != _lastHoverCoord);
        if (positionChanged)
        {
            _lastHoverCoord = gridCoord;
        }

        ClearHoverVisualsOnly();

        bool isValidPlacement = CanPlaceItem(gridCoord.x, gridCoord.y, item.Width, item.Height, item.Data);
        bool anyCellActivated = false;

        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                int targetX = gridCoord.x + x;
                int targetY = gridCoord.y + y;

                if (IsValidCell(targetX, targetY))
                {
                    if (cellViews[targetX, targetY] != null)
                    {
                        cellViews[targetX, targetY].SetHover(true, isValidPlacement);
                        currentlyHoveredCoords.Add(new Vector2Int(targetX, targetY));
                        anyCellActivated = true;
                    }
                }
            }
        }

        // Play grid activation SFX once as you move onto active tiles
        if (positionChanged && anyCellActivated && cellHoverSfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(cellHoverSfx);
        }
    }

    private void ClearHoverVisualsOnly()
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

    public void ClearHover()
    {
        ClearHoverVisualsOnly();
        _lastHoverCoord = new Vector2Int(-9999, -9999);
    }
    #endregion

    #region Helpers & Conversions
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

    #region Gizmos
    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position - new Vector3((columns * cellSize) / 2f, (rows * cellSize) / 2f, 0f);

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 cellCenter = origin + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);

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