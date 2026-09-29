using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Read-only move / attack / capture / repair / raise cell overlays for a unit.
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
        foreach (var cell in moveCells)
            standCells.Add(cell);

        IReadOnlyList<GridCell> attackStands;
        if (MatchUnitAbilities.HasAbility(definition, "moveOrAttackExclusive"))
            attackStands = hasMoved ? [] : [unit.Cell];
        else
            attackStands = standCells;

        return new MatchUnitActionOverlay
        {
            MoveCells = moveCells,
            AttackRangeCells = CollectAttackRangeFromStands(match, definition, attackStands),
            AttackCells = CollectAttackTargetsFromStands(match, unit, definition, attackStands),
            CaptureCells = CollectCaptureTargetsFromStands(match, unit, definition, standCells),
            RepairCells = CollectRepairTargetsFromStands(match, unit, definition, standCells),
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
                    if (!match.IsInBounds(cell))
                        continue;

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
            CollectAttackTargetsFromCell(match, unit, definition, stand, targets);

        return targets.Count == 0 ? [] : targets.ToArray();
    }

    private static void CollectAttackTargetsFromCell(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        GridCell fromCell,
        HashSet<GridCell> targets)
    {
        foreach (var candidate in match.Units)
        {
            if (candidate.Id == unit.Id || candidate.PlayerIndex == unit.PlayerIndex)
                continue;

            var range = fromCell.ManhattanDistanceTo(candidate.Cell);
            if (range < definition.AttackRangeMin || range > definition.AttackRangeMax)
                continue;

            targets.Add(candidate.Cell);
        }

        if (!MatchUnitAbilities.TryGetAbility(definition, "destroyBuilding", out var destroyAbility))
            return;

        foreach (var building in match.Buildings)
        {
            if (building.IsRuined)
                continue;
            if (!match.ContentCatalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
                continue;
            if (!buildingDefinition.Destroyable)
                continue;
            if (!MatchUnitAbilities.TagsIntersect(destroyAbility.Tags, buildingDefinition.Tags))
                continue;
            if (match.IsOccupiedByUnitPublic(building.Cell, exceptUnitId: unit.Id))
                continue;

            var range = fromCell.ManhattanDistanceTo(building.Cell);
            if (range < definition.AttackRangeMin || range > definition.AttackRangeMax)
                continue;

            targets.Add(building.Cell);
        }
    }

    private static IReadOnlyList<GridCell> CollectCaptureTargetsFromStands(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        var targets = new List<GridCell>();
        foreach (var stand in standCells)
        {
            if (!match.TryGetBuildingAt(stand, out var building))
                continue;
            if (!match.ContentCatalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
                continue;
            if (!MatchUnitAbilities.IsCapturable(building, buildingDefinition))
                continue;
            if (building.OwnerPlayerIndex == unit.PlayerIndex)
                continue;
            if (!MatchUnitAbilities.CanCapture(definition, buildingDefinition))
                continue;

            targets.Add(stand);
        }

        return targets;
    }

    private static IReadOnlyList<GridCell> CollectRepairTargetsFromStands(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        var targets = new List<GridCell>();
        foreach (var stand in standCells)
        {
            if (!match.TryGetBuildingAt(stand, out var building))
                continue;
            if (!building.IsRuined)
                continue;
            if (!match.ContentCatalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
                continue;
            if (!buildingDefinition.Repairable)
                continue;
            if (!MatchUnitAbilities.CanRepair(definition, buildingDefinition))
                continue;

            targets.Add(stand);
        }

        return targets;
    }

    private static IReadOnlyList<GridCell> CollectRaiseTargetsFromStands(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        if (!MatchUnitAbilities.HasAbility(definition, "raiseSkeleton"))
            return [];
        if (match.CountUnitsForPlayer(unit.PlayerIndex) >= match.UnitCap)
            return [];

        var targets = new HashSet<GridCell>();
        foreach (var stand in standCells)
        {
            foreach (var stone in match.Gravestones)
            {
                if (stand.ManhattanDistanceTo(stone.Cell) != 1)
                    continue;
                if (match.IsOccupiedByUnitPublic(stone.Cell, exceptUnitId: unit.Id))
                    continue;

                targets.Add(stone.Cell);
            }
        }

        return targets.Count == 0 ? [] : targets.ToArray();
    }
}
