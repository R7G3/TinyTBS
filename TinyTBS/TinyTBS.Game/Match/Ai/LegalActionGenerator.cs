using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match.Ai;

/// <summary>Enumerates legal atomic actions for the current player on <paramref name="match"/>.</summary>
public static class LegalActionGenerator
{
    public static List<BotAtomicAction> Generate(MatchState match)
    {
        ArgumentNullException.ThrowIfNull(match);

        var actions = new List<BotAtomicAction>(64);
        var tie = 0;

        if (match.IsMatchOver || match.IsPlayerEliminated(match.CurrentPlayer))
            return actions;

        if (match.SelectedUnitId is int selectedId)
        {
            MatchUnit? selected = null;
            foreach (var candidate in match.Units)
            {
                if (candidate.Id == selectedId)
                {
                    selected = candidate;
                    break;
                }
            }

            if (selected is not null
                && selected.IsActive
                && selected.PlayerIndex == match.CurrentPlayer)
            {
                AppendSelectedUnitActions(match, selected, actions, ref tie);
                AppendEndTurn(actions, ref tie);
                return actions;
            }
        }

        foreach (var unit in match.Units)
        {
            if (unit.PlayerIndex != match.CurrentPlayer || !unit.IsActive)
                continue;

            actions.Add(new BotAtomicAction
            {
                Kind = BotAtomicActionKind.SelectUnit,
                UnitId = unit.Id,
                Cell = unit.Cell,
                TieBreak = tie++,
            });
        }

        AppendRecruitActions(match, actions, ref tie);
        AppendEndTurn(actions, ref tie);
        return actions;
    }

    private static void AppendEndTurn(List<BotAtomicAction> actions, ref int tie)
    {
        actions.Add(new BotAtomicAction
        {
            Kind = BotAtomicActionKind.EndTurn,
            TieBreak = tie++,
        });
    }

    private static void AppendSelectedUnitActions(
        MatchState match,
        MatchUnit unit,
        List<BotAtomicAction> actions,
        ref int tie)
    {
        if (!match.ContentCatalog.TryGetUnit(unit.TypeId, out var definition))
            return;

        // Only actions valid from the CURRENT cell. Overlay attack lists include
        // targets reachable after a move — ConfirmAt on those without moving fails,
        // scores like Wait, and shallow search then always "stands still".
        var overlay = MatchUnitActionQueries.Build(match, unit, definition, respectActivationMove: true);

        foreach (var cell in overlay.MoveCells)
        {
            actions.Add(new BotAtomicAction
            {
                Kind = BotAtomicActionKind.ConfirmAt,
                UnitId = unit.Id,
                Cell = cell,
                TieBreak = tie++,
            });
        }

        AppendAttacksFromCurrentCell(match, unit, definition, actions, ref tie);
        AppendRaiseFromCurrentCell(match, unit, definition, actions, ref tie);

        // Capture / repair / wait on the unit's own cell.
        actions.Add(new BotAtomicAction
        {
            Kind = BotAtomicActionKind.ConfirmAt,
            UnitId = unit.Id,
            Cell = unit.Cell,
            TieBreak = tie++,
        });

        actions.Add(new BotAtomicAction
        {
            Kind = BotAtomicActionKind.WaitSelected,
            UnitId = unit.Id,
            Cell = unit.Cell,
            TieBreak = tie++,
        });
    }

    private static void AppendAttacksFromCurrentCell(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        List<BotAtomicAction> actions,
        ref int tie)
    {
        if (MatchUnitAbilities.HasAbility(definition, "moveOrAttackExclusive")
            && unit.HasMovedThisActivation)
        {
            return;
        }

        foreach (var candidate in match.Units)
        {
            if (candidate.Id == unit.Id || candidate.PlayerIndex == unit.PlayerIndex)
                continue;

            var range = unit.Cell.ManhattanDistanceTo(candidate.Cell);
            if (range < definition.AttackRangeMin || range > definition.AttackRangeMax)
                continue;

            actions.Add(new BotAtomicAction
            {
                Kind = BotAtomicActionKind.ConfirmAt,
                UnitId = unit.Id,
                Cell = candidate.Cell,
                TieBreak = tie++,
            });
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

            var range = unit.Cell.ManhattanDistanceTo(building.Cell);
            if (range < definition.AttackRangeMin || range > definition.AttackRangeMax)
                continue;

            actions.Add(new BotAtomicAction
            {
                Kind = BotAtomicActionKind.ConfirmAt,
                UnitId = unit.Id,
                Cell = building.Cell,
                TieBreak = tie++,
            });
        }
    }

    private static void AppendRaiseFromCurrentCell(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        List<BotAtomicAction> actions,
        ref int tie)
    {
        if (!MatchUnitAbilities.HasAbility(definition, "raiseSkeleton"))
            return;
        if (match.CountUnitsForPlayer(unit.PlayerIndex) >= match.UnitCap)
            return;

        foreach (var stone in match.Gravestones)
        {
            if (unit.Cell.ManhattanDistanceTo(stone.Cell) != 1)
                continue;
            if (match.IsOccupiedByUnitPublic(stone.Cell, exceptUnitId: unit.Id))
                continue;

            actions.Add(new BotAtomicAction
            {
                Kind = BotAtomicActionKind.ConfirmAt,
                UnitId = unit.Id,
                Cell = stone.Cell,
                TieBreak = tie++,
            });
        }
    }

    private static void AppendRecruitActions(MatchState match, List<BotAtomicAction> actions, ref int tie)
    {
        foreach (var building in match.Buildings)
        {
            if (!building.AllowsRecruit
                || building.IsRuined
                || building.OwnerPlayerIndex != match.CurrentPlayer)
            {
                continue;
            }

            if (match.IsOccupiedByUnitPublic(building.Cell))
                continue;

            foreach (var offer in match.ContentCatalog.ShopOffers)
            {
                if (!match.ContentCatalog.TryGetUnit(offer.UnitTypeId, out _))
                    continue;
                if (match.GetMoney(match.CurrentPlayer) < offer.Cost)
                    continue;
                if (match.IsUniqueUnitOwnedByCurrentPlayer(offer.UnitTypeId))
                    continue;

                actions.Add(new BotAtomicAction
                {
                    Kind = BotAtomicActionKind.Recruit,
                    Cell = building.Cell,
                    UnitTypeId = offer.UnitTypeId,
                    RecruitCost = offer.Cost,
                    TieBreak = tie++,
                });
            }
        }
    }
}
