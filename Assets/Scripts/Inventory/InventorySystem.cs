using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    private readonly Dictionary<WeaponData, int> _counts = new Dictionary<WeaponData, int>();

    public event Action<WeaponData, int> OnCountChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void ResetInventory()
    {
        _counts.Clear();
    }

    public void AddWeapon(WeaponData data, int amount = 1)
    {
        _counts.TryGetValue(data, out int current);
        _counts[data] = current + amount;
        OnCountChanged?.Invoke(data, _counts[data]);
    }

    public int GetCount(WeaponData data)
    {
        return _counts.TryGetValue(data, out int c) ? c : 0;
    }

    public IReadOnlyDictionary<WeaponData, int> GetAllCounts()
    {
        return _counts;
    }

    public bool HasAnyValidMove()
    {
        foreach (var kvp in _counts)
        {
            if (kvp.Value > 0 && WeaponGridManager.Instance.HasAnyEmptyCellFor(kvp.Key))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Places weapon from inventory onto the grid. Links visual object before triggering effects.
    /// </summary>
    public WeaponInstance PlaceFromInventory(WeaponData data, Vector2Int origin, DragDrop visual = null)
    {
        if (GetCount(data) <= 0) return null;
        if (StageManager.Instance.Result != StageResult.InProgress) return null;

        var instance = WeaponGridManager.Instance.TryPlace(data, origin);
        if (instance == null) return null;

        // Link visual object so effects and Hourglass chain reactions can control it
        if (visual != null)
        {
            instance.VisualObject = visual;
        }

        _counts[data]--;
        OnCountChanged?.Invoke(data, _counts[data]);

        WeaponEffectsSystem.Instance.ResolvePlacement(instance);
        StageManager.Instance.CheckForEndOfStage();

        return instance;
    }
}