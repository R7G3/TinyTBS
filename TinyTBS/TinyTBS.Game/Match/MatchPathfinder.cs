namespace TinyTBS.Game.Match;

/// <summary>Reachable cells for a unit given Speed and terrain step costs (BFS).</summary>
public static class MatchPathfinder
{
    private static readonly (int Dx, int Dy)[] Ortho =
    [
        (1, 0), (-1, 0), (0, 1), (0, -1),
    ];

    public static bool CanReach(
        MatchState match,
        MatchUnit unit,
        GridCell destination,
        string movementClass,
        int speed,
        int? exceptUnitId = null)
    {
        if (destination == unit.Cell)
            return true;

        foreach (var cell in CollectReachable(match, unit, movementClass, speed, exceptUnitId))
        {
            if (cell == destination)
                return true;
        }

        return false;
    }

    /// <summary>
    /// All cells the unit can end a move on (excluding its current cell),
    /// spending at most <paramref name="speed"/> with terrain step costs.
    /// </summary>
    public static IReadOnlyList<GridCell> CollectReachable(
        MatchState match,
        MatchUnit unit,
        string movementClass,
        int speed,
        int? exceptUnitId = null)
    {
        if (speed <= 0)
            return [];

        var excludeId = exceptUnitId ?? unit.Id;
        var best = new Dictionary<GridCell, int> { [unit.Cell] = 0 };
        var queue = new Queue<GridCell>();
        queue.Enqueue(unit.Cell);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var spent = best[current];

            foreach (var (dx, dy) in Ortho)
            {
                var next = new GridCell(current.X + dx, current.Y + dy);
                if (!match.IsInBounds(next))
                    continue;
                if (match.IsOccupiedByUnitPublic(next, excludeId))
                    continue;

                var step = MatchTerrainRules.StepCost(match.GetTerrain(next), movementClass);
                var total = spent + step;
                if (total > speed)
                    continue;

                if (best.TryGetValue(next, out var known) && known <= total)
                    continue;

                best[next] = total;
                queue.Enqueue(next);
            }
        }

        var reachable = new List<GridCell>(best.Count);
        foreach (var (cell, _) in best)
        {
            if (cell != unit.Cell)
                reachable.Add(cell);
        }

        return reachable;
    }
}
