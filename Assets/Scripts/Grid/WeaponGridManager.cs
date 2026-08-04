using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponGridManager : MonoBehaviour
{
    public static WeaponGridManager Instance { get; private set; }

    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    [Header("Visual Placeholder")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 gridOrigin = new Vector2(-2f, -2f);

    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color attackColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color modifierColor = new Color(1f, 0.84f, 0.3f);

    private readonly List<WeaponInstance> _placedWeapons = new List<WeaponInstance>();
    private readonly Dictionary<WeaponInstance, List<GameObject>> _visuals = new Dictionary<WeaponInstance, List<GameObject>>();
    private int _nextSequenceIndex;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Resizes the backend grid dimensions and clears placed weapons.
    /// </summary>
    public void SetGridSize(int width, int height)
    {
        gridWidth = width;
        gridHeight = height;
        ResetGrid();
    }

    public void ResetGrid()
    {
        foreach (var objs in _visuals.Values)
            foreach (var go in objs)
                Destroy(go);
        _visuals.Clear();

        _placedWeapons.Clear();
        _nextSequenceIndex = 0;
    }

    public bool CanPlace(WeaponData data, Vector2Int origin)
    {
        var candidate = new WeaponInstance(data, origin, -1);

        foreach (var cell in candidate.OccupiedCells)
        {
            if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight)
                return false;

            if (_placedWeapons.Any(w => w.OccupiedCells.Contains(cell)))
                return false;
        }
        return true;
    }

    public WeaponInstance TryPlace(WeaponData data, Vector2Int origin)
    {
        if (!CanPlace(data, origin)) return null;

        var instance = new WeaponInstance(data, origin, _nextSequenceIndex++);
        _placedWeapons.Add(instance);
        return instance;
    }

    public void Remove(WeaponInstance instance)
    {
        _placedWeapons.Remove(instance);

        if (_visuals.TryGetValue(instance, out var objs))
        {
            foreach (var go in objs)
                Destroy(go);
            _visuals.Remove(instance);
        }
    }

    public IEnumerable<WeaponInstance> GetNeighborsOf(WeaponInstance instance)
    {
        return _placedWeapons.Where(w => w != instance && w.IsAdjacentTo(instance));
    }

    public bool HasAnyEmptyCellFor(WeaponData data)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (CanPlace(data, new Vector2Int(x, y)))
                    return true;
            }
        }
        return false;
    }

    public IReadOnlyList<WeaponInstance> AllWeapons => _placedWeapons;
}