namespace TinyTBS.Game.Match.Ai;

/// <summary>
/// Atomic-action αβ search. Depth is in actions (plies), not full player turns.
/// Easy ≈ shallow / greedy-like; Normal adds depth + quiescence. A future Hard policy
/// can implement <see cref="IBotSearchPolicy"/> with full-turn plans without replacing this.
/// </summary>
public sealed class AtomicAlphaBetaSearch : IBotSearchPolicy
{
    public BotAtomicAction ChooseAction(MatchState state, int botPlayerIndex, BotDifficultyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(profile);

        var rootActions = LegalActionGenerator.Generate(state);
        if (rootActions.Count == 0)
        {
            return new BotAtomicAction
            {
                Kind = BotAtomicActionKind.EndTurn,
                TieBreak = 0,
            };
        }

        // Hard filter: never stand still / end turn while a real action exists.
        // Score penalties alone were not enough — shallow eval often ties and Wait won.
        // Easy may still Wait when only repositioning is left (less aggressive).
        var candidates = PreferProductive(state, rootActions, profile);
        var nodes = 0;
        BotAtomicAction? bestAction = null;
        var bestScore = int.MinValue;

        foreach (var action in candidates.OrderBy(a => a.TieBreak))
        {
            if (nodes >= profile.NodeLimit)
                break;

            var child = state.CloneForAi();
            BotAtomicActionApplicator.Apply(child, action);
            nodes++;

            var score = Negamax(
                child,
                botPlayerIndex,
                depthLeft: Math.Max(0, profile.MaxDepth - 1),
                quiescenceLeft: profile.UseQuiescence && BotAtomicActionApplicator.IsNoisyForQuiescence(action)
                    ? profile.QuiescencePlies
                    : 0,
                alpha: int.MinValue + 1,
                beta: int.MaxValue - 1,
                profile,
                ref nodes);

            if (bestAction is null || score > bestScore
                || (score == bestScore && action.TieBreak < bestAction.TieBreak))
            {
                bestScore = score;
                bestAction = action;
            }
        }

        return bestAction ?? candidates[^1];
    }

    /// <summary>
    /// Drops Wait / idle Confirm / EndTurn when the bot still has moves, attacks, captures, or recruits.
    /// </summary>
    internal static List<BotAtomicAction> PreferProductive(
        MatchState state,
        List<BotAtomicAction> actions,
        BotDifficultyProfile profile)
    {
        var hasFightOrRecruit = false;
        foreach (var action in actions)
        {
            if (action.Kind == BotAtomicActionKind.Recruit
                || (action.Kind == BotAtomicActionKind.ConfirmAt && IsFightOrCaptureConfirm(state, action)))
            {
                hasFightOrRecruit = true;
                break;
            }
        }

        var productive = new List<BotAtomicAction>(actions.Count);
        foreach (var action in actions)
        {
            if (IsProductive(state, action, profile, hasFightOrRecruit))
                productive.Add(action);
        }

        return productive.Count > 0 ? productive : actions;
    }

    internal static bool IsProductive(
        MatchState state,
        BotAtomicAction action,
        BotDifficultyProfile profile,
        bool hasFightOrRecruit)
    {
        switch (action.Kind)
        {
            case BotAtomicActionKind.SelectUnit:
            case BotAtomicActionKind.Recruit:
                return true;
            case BotAtomicActionKind.ConfirmAt:
                return !IsIdleConfirmOnOwnCell(state, action);
            case BotAtomicActionKind.WaitSelected:
                // Easy: Wait may beat a long march when only moves remain.
                return profile.AllowWaitWithMoves && !hasFightOrRecruit;
            case BotAtomicActionKind.EndTurn:
                return false;
            default:
                return false;
        }
    }

    private static bool IsFightOrCaptureConfirm(MatchState state, BotAtomicAction action)
    {
        if (action.Kind != BotAtomicActionKind.ConfirmAt || state.SelectedUnitId is not int selectedId)
            return false;

        MatchUnit? selected = null;
        foreach (var unit in state.Units)
        {
            if (unit.Id == selectedId)
            {
                selected = unit;
                break;
            }
        }

        if (selected is null)
            return false;

        // Attack / destroy / raise: Confirm on another cell that is not a move destination only —
        // treat non-own-cell confirms that aren't pure moves as fight. Own-cell capture/repair too.
        if (selected.Cell == action.Cell)
            return !IsIdleConfirmOnOwnCell(state, action);

        if (state.TryGetUnitAt(action.Cell, out var target) && target.PlayerIndex != state.CurrentPlayer)
            return true;
        if (state.TryGetBuildingAt(action.Cell, out _))
            return true;

        foreach (var stone in state.Gravestones)
        {
            if (stone.Cell == action.Cell)
                return true;
        }

        return false;
    }

    private static bool IsIdleConfirmOnOwnCell(MatchState state, BotAtomicAction action)
    {
        if (action.Kind != BotAtomicActionKind.ConfirmAt || state.SelectedUnitId is not int selectedId)
            return false;

        MatchUnit? selected = null;
        foreach (var unit in state.Units)
        {
            if (unit.Id == selectedId)
            {
                selected = unit;
                break;
            }
        }

        if (selected is null || selected.Cell != action.Cell)
            return false;

        // Confirm on own cell finishes as Wait unless capture/repair applies.
        if (!state.ContentCatalog.TryGetUnit(selected.TypeId, out var definition))
            return true;
        if (!state.TryGetBuildingAt(selected.Cell, out var building))
            return true;
        if (!state.ContentCatalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
            return true;

        if (building.IsRuined
            && buildingDefinition.Repairable
            && MatchUnitAbilities.CanRepair(definition, buildingDefinition))
        {
            return false;
        }

        if (!building.IsRuined
            && !building.RepairedThisOwnerTurn
            && MatchUnitAbilities.IsCapturable(building, buildingDefinition)
            && building.OwnerPlayerIndex != state.CurrentPlayer
            && MatchUnitAbilities.CanCapture(definition, buildingDefinition))
        {
            return false;
        }

        return true;
    }

    private static int Negamax(
        MatchState state,
        int botPlayerIndex,
        int depthLeft,
        int quiescenceLeft,
        int alpha,
        int beta,
        BotDifficultyProfile profile,
        ref int nodes)
    {
        if (nodes >= profile.NodeLimit || state.IsMatchOver)
            return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);

        var maximizing = state.CurrentPlayer == botPlayerIndex;
        if (depthLeft <= 0 && quiescenceLeft <= 0)
            return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);

        var actions = LegalActionGenerator.Generate(state);
        if (actions.Count == 0)
            return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);

        // Inside the tree also skip pure idle when something better exists,
        // so shallow lines do not "Wait forever" after a Select.
        if (maximizing)
            actions = PreferProductive(state, actions, profile);

        if (depthLeft <= 0 && quiescenceLeft > 0)
        {
            actions = actions
                .Where(action =>
                    action.Kind == BotAtomicActionKind.EndTurn
                    || action.Kind == BotAtomicActionKind.WaitSelected
                    || BotAtomicActionApplicator.IsNoisyForQuiescence(action))
                .OrderBy(action => action.TieBreak)
                .ToList();
            if (actions.Count == 0)
                return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);
        }
        else
        {
            actions = actions.OrderBy(action => action.TieBreak).ToList();
        }

        var best = maximizing ? int.MinValue + 1 : int.MaxValue - 1;

        foreach (var action in actions)
        {
            if (nodes >= profile.NodeLimit)
                break;

            var child = state.CloneForAi();
            BotAtomicActionApplicator.Apply(child, action);
            nodes++;

            var nextDepth = depthLeft > 0 ? depthLeft - 1 : 0;
            var nextQuiescence = depthLeft > 0
                ? (profile.UseQuiescence && BotAtomicActionApplicator.IsNoisyForQuiescence(action)
                    ? profile.QuiescencePlies
                    : 0)
                : Math.Max(0, quiescenceLeft - 1);

            var score = Negamax(
                child,
                botPlayerIndex,
                nextDepth,
                nextQuiescence,
                alpha,
                beta,
                profile,
                ref nodes);

            if (maximizing)
            {
                if (score > best)
                    best = score;
                if (best > alpha)
                    alpha = best;
            }
            else
            {
                if (score < best)
                    best = score;
                if (best < beta)
                    beta = best;
            }

            if (alpha >= beta)
                break;
        }

        return best;
    }
}
