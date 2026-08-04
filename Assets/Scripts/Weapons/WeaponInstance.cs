using System.Collections.Generic;
using UnityEngine;

// Wraps a WeaponData with runtime state (position, sequence order, resolved damage).
// SequenceIndex = urutan ke berapa weapon ini ditaro di grid pada stage ini.
// Ini yang dipakai Book untuk tahu "siapa yang ditaro duluan" (order matters).
public class WeaponInstance
{
    public WeaponData Data { get; }
    public Vector2Int OriginCell { get; private set; }
    public int SequenceIndex { get; private set; }

    // Damage final yang sudah dihitung + buff yang aktif SAAT weapon ini ditaro.
    // Tidak berubah lagi setelahnya walau ada Book baru ditaro belakangan
    // (karena attack sudah keburu terjadi, sesuai aturan order).
    public int ResolvedDamage { get; set; }

    public List<Vector2Int> OccupiedCells { get; } = new List<Vector2Int>();

    public WeaponInstance(WeaponData data, Vector2Int originCell, int sequenceIndex)
    {
        Data = data;
        SequenceIndex = sequenceIndex;
        ResolvedDamage = data.baseDamage;
        PlaceAt(originCell);
    }

    public void PlaceAt(Vector2Int cell)
    {
        OriginCell = cell;
        OccupiedCells.Clear();
        OccupiedCells.AddRange(Data.GetOccupiedCells(cell));
    }

    public bool IsAdjacentTo(WeaponInstance other)
    {
        foreach (var cellA in OccupiedCells)
        {
            foreach (var cellB in other.OccupiedCells)
            {
                if (Vector2Int.Distance(cellA, cellB) <= 1.01f)
                    return true;
            }
        }
        return false;
    }
}
