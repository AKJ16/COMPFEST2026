using System.Collections.Generic;
using UnityEngine;

// Builds an irregular but always-connected set of playable cells, bounded by
// a max width/height box (e.g. 3x5). Doesn't have to fill the whole rectangle.
public static class GridShapeGenerator
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    // cellCount = how many playable squares the stage should have (this is also
    // the turn count you pass to TurnManager.StartStage). Must fit within maxWidth*maxHeight.
    public static HashSet<Vector2Int> Generate(int maxWidth, int maxHeight, int cellCount)
    {
        cellCount = Mathf.Clamp(cellCount, 1, maxWidth * maxHeight);

        var shape = new HashSet<Vector2Int>();
        var frontier = new List<Vector2Int>();

        Vector2Int start = new Vector2Int(maxWidth / 2, maxHeight / 2);
        shape.Add(start);
        AddFrontierNeighbors(start, shape, frontier, maxWidth, maxHeight);

        // Random growth: repeatedly pop a random cell from the frontier and commit it,
        // then queue its own neighbors. Guarantees every added cell touches an existing
        // one, so the result is always a single connected region.
        while (shape.Count < cellCount && frontier.Count > 0)
        {
            int index = Random.Range(0, frontier.Count);
            Vector2Int next = frontier[index];
            frontier.RemoveAt(index);

            if (shape.Contains(next)) continue;

            shape.Add(next);
            AddFrontierNeighbors(next, shape, frontier, maxWidth, maxHeight);
        }

        return shape;
    }

    private static void AddFrontierNeighbors(Vector2Int cell, HashSet<Vector2Int> shape,
        List<Vector2Int> frontier, int maxWidth, int maxHeight)
    {
        foreach (var dir in Directions)
        {
            Vector2Int neighbor = cell + dir;

            if (neighbor.x < 0 || neighbor.x >= maxWidth || neighbor.y < 0 || neighbor.y >= maxHeight)
                continue;

            if (!shape.Contains(neighbor) && !frontier.Contains(neighbor))
                frontier.Add(neighbor);
        }
    }
}
