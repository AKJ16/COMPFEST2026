using System.Collections.Generic;
using UnityEngine;

public class WeaponInstance
{
    public WeaponData Data { get; }
    public Vector2Int OriginCell { get; private set; }
    public int SequenceIndex { get; private set; }

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

    /// <summary>
    /// Checks if this weapon is adjacent to another weapon (includes Up, Down, Left, Right, and Diagonals/Edges).
    /// </summary>
    public bool IsAdjacentTo(WeaponInstance other)
    {
        foreach (var cellA in OccupiedCells)
        {
            foreach (var cellB in other.OccupiedCells)
            {
                int dx = Mathf.Abs(cellA.x - cellB.x);
                int dy = Mathf.Abs(cellA.y - cellB.y);

                // 8-directional neighbor: within 1 cell in any direction (including diagonal corners)
                if (dx <= 1 && dy <= 1 && !(dx == 0 && dy == 0))
                    return true;
            }
        }
        return false;
    }
}