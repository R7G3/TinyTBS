using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Read-only move / attack / capture / repair / raise cell overlays for a unit, built from the same
/// predicates as the rules (<see cref="MatchActionRules"/>) evaluated from every cell the unit could stand on.
/// </summary>
public static class MatchUnitActionQueries
{
    public static MatchUnitActionOverlay Build(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        bool respectActivationMove)
    {
        var hasMoved = respectActivationMove && unit.HasMovedThisActivation;
        var moveCells = hasMoved
            ? (IReadOnlyList<GridCell>)[]
            : MatchPathfinder.CollectReachable(
                match,
                unit,
                definition.MovementClass,
                definition.Speed,
                unit.Id);

        var standCells = new List<GridCell>(moveCells.Count + 1) { unit.Cell };
        standCells.AddRange(moveCells);

        IReadOnlyList<GridCell> attackStands;
        if (MatchUnitAbilities.HasAbility(definition, UnitAbilityTypes.MoveOrAttackExclusive))
            attackStands = hasMoved ? [] : [unit.Cell];
        else
            attackStands = standCells;

        return new MatchUnitActionOverlay
        {
            MoveCells = moveCells,
            AttackRangeCells = CollectAttackRangeFromStands(match, definition, attackStands),
            AttackCells = CollectAttackTargetsFromStands(match, unit, definition, attackStands),
            CaptureCells = standCells
                .Where(stand => MatchActionRules.CanCaptureAt(match, unit, definition, stand))
                .ToArray(),
            RepairCells = standCells
                .Where(stand => MatchActionRules.CanRepairAt(match, unit, definition, stand))
                .ToArray(),
            RaiseCells = CollectRaiseTargetsFromStands(match, unit, definition, standCells),
        };
    }

    private static IReadOnlyList<GridCell> CollectAttackRangeFromStands(
        MatchState match,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        if (standCells.Count == 0)
            return [];

        var rangeMin = definition.AttackRangeMin;
        var rangeMax = definition.AttackRangeMax;
        if (rangeMax < rangeMin || rangeMax < 0)
            return [];

        var cells = new HashSet<GridCell>();
        foreach (var stand in standCells)
        {
            for (var dy = -rangeMax; dy <= rangeMax; dy++)
            {
                for (var dx = -rangeMax; dx <= rangeMax; dx++)
                {
                    var distance = Math.Abs(dx) + Math.Abs(dy);
                    if (distance < rangeMin || distance > rangeMax)
                        continue;

                    var cell = new GridCell(stand.X + dx, stand.Y + dy);
                    if (match.IsInBounds(cell))
                        cells.Add(cell);
                }
            }
        }

        return cells.Count == 0 ? [] : cells.ToArray();
    }

    private static IReadOnlyList<GridCell> CollectAttackTargetsFromStands(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        var targets = new HashSet<GridCell>();
        foreach (var stand in standCells)
        {
            foreach (var candidate in match.Units)
            {
                if (MatchActionRules.IsAttackTargetFrom(match, unit, definition, stand, candidate))
                    targets.Add(candidate.Cell);
            }

            foreach (var building in match.Buildings)
            {
                if (MatchActionRules.IsDestroyTargetFrom(match, unit, definition, stand, building))
                    targets.Add(building.Cell);
            }
        }

        return targets.Count == 0 ? [] : targets.ToArray();
    }

    private static IReadOnlyList<GridCell> CollectRaiseTargetsFromStands(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        var targets = new HashSet<GridCell>();
        foreach (var stand in standCells)
        {
            foreach (var stone in match.Gravestones)
            {
                if (MatchActionRules.IsRaiseTargetFrom(match, unit, definition, stand, stone.Cell))
                    targets.Add(stone.Cell);
            }
        }

        return targets.Count == 0 ? [] : targets.ToArray();
    }
}
