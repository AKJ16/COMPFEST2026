using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    private readonly Dictionary<WeaponData, int> _counts = new Dictionary<WeaponData, int>();
    private readonly Dictionary<WeaponData, int> _stageUsage = new Dictionary<WeaponData, int>();

    public event Action<WeaponData, int> OnCountChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void ResetInventory()
    {
        _counts.Clear();
        _stageUsage.Clear();
    }

    /// <summary>
    /// Resets the book usage limits at the start of every stage.
    /// </summary>
    public void ResetStageUsage()
    {
        _stageUsage.Clear();
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
            // Do NOT count weapons that have reached their stage limit!
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

        // Increment stage placement usage
        _stageUsage[data] = GetUsage(data) + 1;

        _counts[data]--;
        OnCountChanged?.Invoke(data, _counts[data]);

        WeaponEffectsSystem.Instance.ResolvePlacement(instance);

        return instance;
    }
}