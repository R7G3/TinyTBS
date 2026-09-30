namespace TinyTBS.Rules.Match;

/// <summary>
/// What a Confirm (click / A) on a cell means for the current player, with the precedence players see:
/// select → own cell (repair, capture, else wait) → attack → destroy → raise → move → reselect.
/// </summary>
public static class MatchActionResolver
{
    public static MatchAction? ResolveConfirm(MatchState state, GridCell cell, int? selectedUnitId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.IsMatchOver || state.IsPlayerEliminated(state.CurrentPlayer))
            return null;

        if (selectedUnitId is not int actingUnitId)
        {
            return state.TryGetUnitAt(cell, out var candidate) && MatchActionRules.CanSelect(state, candidate.Id, selectedUnitId)
                ? MatchAction.SelectUnit(candidate.Id, candidate.Cell)
                : null;
        }

        if (!MatchActionRules.TryGetActingUnit(state, actingUnitId, selectedUnitId, out var unit, out var definition))
            return null;

        if (cell == unit.Cell)
        {
            if (MatchActionRules.CanRepairAt(state, unit, definition, cell))
                return MatchAction.RepairBuilding(unit.Id, cell);
            if (MatchActionRules.CanCaptureAt(state, unit, definition, cell))
                return MatchAction.CaptureBuilding(unit.Id, cell);
            return MatchAction.WaitUnit(unit.Id, cell);
        }

        if (MatchActionRules.CanAttack(state, unit, definition, cell))
            return MatchAction.AttackUnit(unit.Id, cell);
        if (MatchActionRules.CanDestroyBuilding(state, unit, definition, cell))
            return MatchAction.DestroyBuilding(unit.Id, cell);
        if (MatchActionRules.CanRaiseSkeleton(state, unit, definition, cell))
            return MatchAction.RaiseSkeleton(unit.Id, cell);
        if (MatchActionRules.CanMove(state, unit, definition, cell))
            return MatchAction.MoveUnit(unit.Id, cell);

        return state.TryGetUnitAt(cell, out var other) && MatchActionRules.CanSelect(state, other.Id, selectedUnitId)
            ? MatchAction.SelectUnit(other.Id, other.Cell)
            : null;
    }
}
