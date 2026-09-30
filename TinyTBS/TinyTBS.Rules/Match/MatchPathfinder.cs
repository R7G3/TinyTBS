using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Rules.Match;

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
        MovementClass movementClass,
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
        MovementClass movementClass,
        int speed,
        int? exceptUnitId = null)
    {
        if (speed <= 0)
            return [];

        var excludeId = exceptUnitId ?? unit.Id;
        RunCostSearch(
            match,
            unit.Cell,
            movementClass,
            speed,
            excludeId,
            out var best,
            out _);

        var reachable = new List<GridCell>(best.Count);
        foreach (var (cell, _) in best)
        {
            if (cell != unit.Cell)
                reachable.Add(cell);
        }

        return reachable;
    }

    /// <summary>
    /// Cheapest ortho path from <paramref name="from"/> to <paramref name="to"/> within
    /// <paramref name="speed"/> (terrain step costs). Includes both endpoints.
    /// Prefers roads/bridges over forest/mountain/water because their step cost is lower.
    /// </summary>
    public static IReadOnlyList<GridCell> FindCheapestPath(
        MatchState match,
        GridCell from,
        GridCell to,
        MovementClass movementClass,
        int speed,
        int? exceptUnitId = null)
    {
        ArgumentNullException.ThrowIfNull(match);

        if (from == to)
            return [from];

        if (speed <= 0)
            return BuildManhattanFallback(from, to);

        RunCostSearch(
            match,
            from,
            movementClass,
            speed,
            exceptUnitId,
            out var best,
            out var parent);

        if (!best.ContainsKey(to))
            return BuildManhattanFallback(from, to);

        var reverse = new List<GridCell>();
        var cursor = to;
        while (true)
        {
            reverse.Add(cursor);
            if (cursor == from)
                break;
            if (!parent.TryGetValue(cursor, out var previous))
                return BuildManhattanFallback(from, to);
            cursor = previous;
        }

        reverse.Reverse();
        return reverse;
    }

    private static void RunCostSearch(
        MatchState match,
        GridCell start,
        MovementClass movementClass,
        int speed,
        int? exceptUnitId,
        out Dictionary<GridCell, int> best,
        out Dictionary<GridCell, GridCell> parent)
    {
        best = new Dictionary<GridCell, int> { [start] = 0 };
        parent = new Dictionary<GridCell, GridCell>();
        var queue = new Queue<GridCell>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var spent = best[current];

            foreach (var (dx, dy) in Ortho)
            {
                var next = new GridCell(current.X + dx, current.Y + dy);
                if (!match.IsInBounds(next))
                    continue;
                if (match.IsOccupiedByUnit(next, exceptUnitId))
                    continue;

                var step = MatchTerrainRules.StepCost(match.GetTerrain(next), movementClass);
                var total = spent + step;
                if (total > speed)
                    continue;

                if (best.TryGetValue(next, out var known) && known <= total)
                    continue;

                best[next] = total;
                parent[next] = current;
                queue.Enqueue(next);
            }
        }
    }

    /// <summary>Visual-only fallback: horizontal then vertical (no terrain awareness).</summary>
    private static IReadOnlyList<GridCell> BuildManhattanFallback(GridCell from, GridCell to)
    {
        var path = new List<GridCell> { from };
        var x = from.X;
        var y = from.Y;
        while (x != to.X)
        {
            x += Math.Sign(to.X - x);
            path.Add(new GridCell(x, y));
        }

        while (y != to.Y)
        {
            y += Math.Sign(to.Y - y);
            path.Add(new GridCell(x, y));
        }

        return path;
    }
}
