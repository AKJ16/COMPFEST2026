using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Central authority for the grid board. Setiap TryPlace dikasih SequenceIndex
// berurutan (0, 1, 2, ...) — ini penting karena buff (Book) dan attack timing
// bergantung pada urutan taro, bukan cuma posisi.
public class WeaponGridManager : MonoBehaviour
{
    public static WeaponGridManager Instance { get; private set; }

    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;

    // Dipakai UI test grid (dan nanti Adriel) buat tau ukuran board tanpa duplikat angka.
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    [Header("Visual Placeholder")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 gridOrigin = new Vector2(-2f, -2f); // posisi world buat grid cell (0,0)

    // Warna fallback kalau Category weapon-nya nggak dikenali
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color attackColor = new Color(0.6f, 0.6f, 0.6f);   // abu-abu
    [SerializeField] private Color modifierColor = new Color(1f, 0.84f, 0.3f);  // kuning keemasan

    private readonly List<WeaponInstance> _placedWeapons = new List<WeaponInstance>();
    private readonly Dictionary<WeaponInstance, List<GameObject>> _visuals = new Dictionary<WeaponInstance, List<GameObject>>();
    private int _nextSequenceIndex;

    private void Awake()
    {
        Instance = this;
    }

    // Panggil ini setiap mulai stage baru supaya sequence & grid ke-reset.
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
        SpawnVisual(instance);
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

    // Bikin 1 GameObject visual per cell yang ditempatin weapon ini,
    // pakai Icon dari WeaponData masing-masing + warna berdasarkan Category.
    private void SpawnVisual(WeaponInstance instance)
    {
        var data = instance.Data;
        var cellObjects = new List<GameObject>();

        foreach (var cell in instance.OccupiedCells)
        {
            var go = new GameObject($"{data.name}_Cell");
            go.transform.position = GridToWorld(cell);
            go.transform.localScale = Vector3.one * (cellSize * 0.9f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = data.icon; // sprite placeholder dari asset weapon-nya sendiri
            sr.color = GetColorFor(data);

            cellObjects.Add(go);
        }
        _visuals[instance] = cellObjects;
    }

    private Color GetColorFor(WeaponData data)
    {
        switch (data.category)
        {
            case WeaponCategory.Attack:
                return attackColor;
            case WeaponCategory.Modifier:
                return modifierColor;
            default:
                return defaultColor;
        }
    }

    private Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(gridOrigin.x + cell.x * cellSize, gridOrigin.y + cell.y * cellSize, 0f);
    }

    public IEnumerable<WeaponInstance> GetNeighborsOf(WeaponInstance instance)
    {
        return _placedWeapons.Where(w => w != instance && w.IsAdjacentTo(instance));
    }

    // Dipakai StageManager buat tahu "masih ada kemungkinan gerak atau enggak".
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