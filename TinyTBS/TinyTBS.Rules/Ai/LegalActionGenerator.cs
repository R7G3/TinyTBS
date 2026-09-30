using TinyTBS.Rules.Match;
using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Rules.Ai;

/// <summary>
/// Lists the current player's atomic decisions — the branches of the search tree. Candidate cells come from
/// the rules' own predicates and every Confirm is resolved by <see cref="MatchActionResolver"/>, so the bot
/// can only choose what a human could do with the same click.
/// </summary>
public static class LegalActionGenerator
{
    public static List<BotAtomicAction> Generate(MatchState match)
    {
        ArgumentNullException.ThrowIfNull(match);

        var actions = new List<BotAtomicAction>(64);
        var tieBreak = 0;

        if (match.IsMatchOver || match.IsPlayerEliminated(match.CurrentPlayer))
            return actions;

        // A selected unit: only its moves / strikes / wait (+ End turn), never switching to another unit.
        if (match.SelectedUnitId is int selectedId
            && match.TryGetUnit(selectedId, out var selected)
            && selected.IsActive
            && selected.PlayerIndex == match.CurrentPlayer)
        {
            AppendSelectedUnitActions(match, selected, actions, ref tieBreak);
            Append(actions, BotAtomicActionKind.EndTurn, MatchAction.EndTurn, ref tieBreak);
            return actions;
        }

        // Nothing selected: pick an active unit, buy in a castle, or end the turn.
        foreach (var unit in match.Units)
        {
            if (unit.PlayerIndex != match.CurrentPlayer || !unit.IsActive)
                continue;

            Append(actions, BotAtomicActionKind.SelectUnit, MatchAction.SelectUnit(unit.Id, unit.Cell), ref tieBreak);
        }

        AppendRecruitActions(match, actions, ref tieBreak);
        Append(actions, BotAtomicActionKind.EndTurn, MatchAction.EndTurn, ref tieBreak);
        return actions;
    }

    private static void AppendSelectedUnitActions(
        MatchState match,
        MatchUnit unit,
        List<BotAtomicAction> actions,
        ref int tieBreak)
    {
        if (!match.ContentCatalog.TryGetUnit(unit.TypeId, out var definition))
            return;

        var overlay = MatchUnitActionQueries.Build(match, unit, definition, respectActivationMove: true);
        foreach (var cell in overlay.MoveCells)
            AppendConfirm(match, cell, actions, ref tieBreak);

        // Strikes only from the current cell: the overlay also shows targets after a possible move,
        // and a Confirm there without moving would do nothing.
        AppendStrikesFromCurrentCell(match, unit, definition, actions, ref tieBreak);
        AppendRaisesFromCurrentCell(match, unit, definition, actions, ref tieBreak);

        // Confirm on the own cell: capture / repair, otherwise wait.
        AppendConfirm(match, unit.Cell, actions, ref tieBreak);
        Append(actions, BotAtomicActionKind.WaitSelected, MatchAction.WaitUnit(unit.Id, unit.Cell), ref tieBreak);
    }

    private static void AppendStrikesFromCurrentCell(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        List<BotAtomicAction> actions,
        ref int tieBreak)
    {
        if (MatchActionRules.IsAttackLockedAfterMove(unit, definition))
            return;

        foreach (var candidate in match.Units)
        {
            if (MatchActionRules.IsAttackTargetFrom(match, unit, definition, unit.Cell, candidate))
                AppendConfirm(match, candidate.Cell, actions, ref tieBreak);
        }

        foreach (var building in match.Buildings)
        {
            if (MatchActionRules.IsDestroyTargetFrom(match, unit, definition, unit.Cell, building))
                AppendConfirm(match, building.Cell, actions, ref tieBreak);
        }
    }

    private static void AppendRaisesFromCurrentCell(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        List<BotAtomicAction> actions,
        ref int tieBreak)
    {
        foreach (var stone in match.Gravestones)
        {
            if (MatchActionRules.IsRaiseTargetFrom(match, unit, definition, unit.Cell, stone.Cell))
                AppendConfirm(match, stone.Cell, actions, ref tieBreak);
        }
    }

    private static void AppendRecruitActions(MatchState match, List<BotAtomicAction> actions, ref int tieBreak)
    {
        foreach (var building in match.Buildings)
        {
            if (!building.AllowsRecruit
                || building.IsRuined
                || building.OwnerPlayerIndex != match.CurrentPlayer)
            {
                continue;
            }

            foreach (var offer in match.ContentCatalog.ShopOffers)
            {
                var recruit = MatchAction.RecruitUnit(offer.UnitTypeId, building.Cell);
                if (MatchActionRules.IsValid(match, recruit))
                    Append(actions, BotAtomicActionKind.Recruit, recruit, ref tieBreak);
            }
        }
    }

    private static void AppendConfirm(MatchState match, GridCell cell, List<BotAtomicAction> actions, ref int tieBreak)
    {
        if (MatchActionResolver.ResolveConfirm(match, cell) is { } action)
            Append(actions, BotAtomicActionKind.ConfirmAt, action, ref tieBreak);
    }

    private static void Append(
        List<BotAtomicAction> actions,
        BotAtomicActionKind kind,
        MatchAction action,
        ref int tieBreak)
    {
        actions.Add(new BotAtomicAction
        {
            Kind = kind,
            Action = action,
            TieBreak = tieBreak++,
        });
    }
}
