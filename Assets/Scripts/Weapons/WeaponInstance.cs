using System.Collections.Generic;
using UnityEngine;

public class WeaponInstance
{
    public WeaponData Data { get; }
    public Vector2Int OriginCell { get; private set; }
    public int SequenceIndex { get; private set; }
    public int ResolvedDamage { get; set; }

    // Reference to the physical DragDrop object resting on the grid
    public DragDrop VisualObject { get; set; }

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
                int dx = Mathf.Abs(cellA.x - cellB.x);
                int dy = Mathf.Abs(cellA.y - cellB.y);

                if (dx <= 1 && dy <= 1 && !(dx == 0 && dy == 0))
                    return true;
            }
        }
        return false;
    }
}