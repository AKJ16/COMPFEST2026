using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks how many of each weapon the player owns and moves them onto the grid.
// Begitu weapon berhasil ditaro: langsung di-resolve efeknya (attack instan / buff /
// hourglass), lalu dicek apakah stage ini sudah berakhir (menang/kalah).
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

    // Dipakai UI buat nampilin semua weapon yang pernah dimiliki (termasuk yang stoknya 0),
    // supaya urutan tampilan konsisten meski stok habis lalu nambah lagi.
    public IReadOnlyDictionary<WeaponData, int> GetAllCounts()
    {
        return _counts;
    }

    // True kalau masih ada minimal 1 weapon di inventory yang punya slot kosong di grid.
    // Dipakai StageManager buat nentuin "masih bisa gerak atau enggak".
    public bool HasAnyValidMove()
    {
        foreach (var kvp in _counts)
        {
            if (kvp.Value > 0 && WeaponGridManager.Instance.HasAnyEmptyCellFor(kvp.Key))
                return true;
        }
        return false;
    }

    // Moves one unit of the weapon from inventory onto the grid, resolves its effect
    // immediately, then tells StageManager to check win/lose.
    public WeaponInstance PlaceFromInventory(WeaponData data, Vector2Int origin)
    {
        if (GetCount(data) <= 0) return null;
        if (StageManager.Instance.Result != StageResult.InProgress) return null;

        var instance = WeaponGridManager.Instance.TryPlace(data, origin);
        if (instance == null) return null;

        _counts[data]--;
        OnCountChanged?.Invoke(data, _counts[data]);

        WeaponEffectsSystem.Instance.ResolvePlacement(instance);
        StageManager.Instance.CheckForEndOfStage();

        return instance;
    }
}