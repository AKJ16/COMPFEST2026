using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    private readonly Dictionary<WeaponData, int> _counts = new Dictionary<WeaponData, int>();
    private readonly Dictionary<WeaponData, int> _stageUsage = new Dictionary<WeaponData, int>();

    // Tracks weapons the player has actually received with amount > 0
    private readonly HashSet<WeaponData> _unlockedWeapons = new HashSet<WeaponData>();

    public event Action<WeaponData, int> OnCountChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void ResetInventory()
    {
        _counts.Clear();
        _stageUsage.Clear();
        _unlockedWeapons.Clear();
    }

    public void ResetStageUsage()
    {
        _stageUsage.Clear();
    }

    /// <summary>
    /// Returns true if the player has ever actually possessed this weapon (amount > 0).
    /// </summary>
    public bool HasEverPossessed(WeaponData data)
    {
        return data != null && _unlockedWeapons.Contains(data);
    }

    public int GetUsage(WeaponData data)
    {
        return _stageUsage.TryGetValue(data, out int u) ? u : 0;
    }

    public int GetRemainingUses(WeaponData data)
    {
        if (!data.HasUsageCap) return int.MaxValue;
        return Mathf.Max(0, data.MaxUsage - GetUsage(data));
    }

    public bool IsLimitReached(WeaponData data)
    {
        if (!data.HasUsageCap) return false;
        return GetUsage(data) >= data.MaxUsage;
    }

    public void AddWeapon(WeaponData data, int amount = 1)
    {
        if (data == null) return;

        // ONLY unlock/discover if actually granted (amount > 0)
        if (amount > 0)
        {
            _unlockedWeapons.Add(data);
        }

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
            if (kvp.Value > 0 && !IsLimitReached(kvp.Key) && WeaponGridManager.Instance.HasAnyEmptyCellFor(kvp.Key))
                return true;
        }
        return false;
    }

    public WeaponInstance PlaceFromInventory(WeaponData data, Vector2Int origin, DragDrop visual = null)
    {
        if (GetCount(data) <= 0) return null;
        if (IsLimitReached(data)) return null;
        if (StageManager.Instance != null && StageManager.Instance.Result != StageResult.InProgress) return null;

        var instance = WeaponGridManager.Instance.TryPlace(data, origin);
        if (instance == null) return null;

        if (visual != null)
        {
            instance.VisualObject = visual;
        }

        _stageUsage[data] = GetUsage(data) + 1;

        _counts[data]--;
        OnCountChanged?.Invoke(data, _counts[data]);

        WeaponEffectsSystem.Instance.ResolvePlacement(instance);

        return instance;
    }
}