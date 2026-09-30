using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ai;

/// <summary>
/// Chooses the bot's best atomic decision with α-β (negamax). Depth counts atomic decisions
/// (select a unit, step, strike…), not whole player turns. Hard can replace this policy later.
/// </summary>
public sealed class AtomicAlphaBetaSearch : IBotSearchPolicy
{
    public BotAtomicAction ChooseAction(MatchState state, int botPlayerIndex, BotDifficultyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(profile);

        // Every "button" a human has in this position: select / confirm / wait / recruit / end turn.
        var rootActions = LegalActionGenerator.Generate(state);
        if (rootActions.Count == 0)
        {
            return new BotAtomicAction
            {
                Kind = BotAtomicActionKind.EndTurn,
                Action = MatchAction.EndTurn,
                TieBreak = 0,
            };
        }

        // Do not let the bot idle while real moves exist (fight / move / recruit).
        // Easy may still Wait when only repositioning without combat remains.
        var candidates = PreferProductive(state, rootActions, profile);
        var nodes = 0;
        BotAtomicAction? bestAction = null;
        var bestScore = int.MinValue;
        var bestCastleDistance = int.MaxValue;
        var bestSafety = int.MaxValue;
        var opponent = PositionEvaluator.OpponentIndex(state, botPlayerIndex);
        var rootSafety = PositionEvaluator.MeasureKingSafetyPenalty(
            state,
            botPlayerIndex,
            opponent,
            profile.KingSafetyWeight);

        foreach (var action in candidates.OrderBy(candidate => candidate.TieBreak))
        {
            if (nodes >= profile.NodeLimit)
                break;

            var child = state.Clone();
            child.TryApply(action.Action);
            nodes++;

            var score = Negamax(
                child,
                botPlayerIndex,
                depthLeft: Math.Max(0, profile.MaxDepth - 1),
                quiescenceLeft: profile.UseQuiescence && action.IsNoisyForQuiescence
                    ? profile.QuiescencePlies
                    : 0,
                alpha: int.MinValue + 1,
                beta: int.MaxValue - 1,
                profile,
                ref nodes);

            var childCastleDistance = PositionEvaluator.MeasureKingEnemyCastleDistance(child, botPlayerIndex);
            var childSafety = PositionEvaluator.MeasureKingSafetyPenalty(
                child,
                botPlayerIndex,
                opponent,
                profile.KingSafetyWeight);

            if (bestAction is null
                || IsBetterRootCandidate(
                    score,
                    childSafety,
                    childCastleDistance,
                    action.TieBreak,
                    bestScore,
                    bestSafety,
                    bestCastleDistance,
                    bestAction.TieBreak,
                    rootSafety))
            {
                bestScore = score;
                bestAction = action;
                bestCastleDistance = childCastleDistance;
                bestSafety = childSafety;
            }
        }

        return bestAction ?? candidates[^1];
    }

    /// <summary>
    /// Equal-score tie-break: prefer VIP→defeat-building progress only when safety is not worse than root;
    /// otherwise prefer safer, then TieBreak.
    /// </summary>
    internal static bool IsBetterRootCandidate(
        int score,
        int childSafety,
        int childCastleDistance,
        int childTieBreak,
        int bestScore,
        int bestSafety,
        int bestCastleDistance,
        int bestTieBreak,
        int rootSafety)
    {
        if (score > bestScore)
            return true;
        if (score < bestScore)
            return false;

        var childOk = childSafety <= rootSafety;
        var bestOk = bestSafety <= rootSafety;

        if (childOk && !bestOk)
            return true;
        if (!childOk && bestOk)
            return false;

        if (childOk && bestOk)
        {
            if (childCastleDistance != bestCastleDistance)
                return childCastleDistance < bestCastleDistance;
            return childTieBreak < bestTieBreak;
        }

        if (childSafety != bestSafety)
            return childSafety < bestSafety;
        return childTieBreak < bestTieBreak;
    }

    /// <summary>
    /// Keeps only "useful" actions when there are any; otherwise returns the input list
    /// (for example when only Wait / End turn remain).
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
            if (IsProductive(action, profile, hasFightOrRecruit))
                productive.Add(action);
        }

        return productive.Count > 0 ? productive : actions;
    }

    internal static bool IsProductive(
        BotAtomicAction action,
        BotDifficultyProfile profile,
        bool hasFightOrRecruit) =>
        action.Kind switch
        {
            BotAtomicActionKind.SelectUnit or BotAtomicActionKind.Recruit => true,
            // Confirm on the own cell without capture / repair is just standing still.
            BotAtomicActionKind.ConfirmAt => !IsIdleConfirmOnOwnCell(action),
            // Easy may prefer Wait over a long march when there is no fight or recruit.
            BotAtomicActionKind.WaitSelected => profile.AllowWaitWithMoves && !hasFightOrRecruit,
            _ => false,
        };

    private static bool IsFightOrCaptureConfirm(MatchState state, BotAtomicAction action)
    {
        if (action.Kind != BotAtomicActionKind.ConfirmAt
            || state.SelectedUnitId is not int selectedId
            || !state.TryGetUnit(selectedId, out var selected))
        {
            return false;
        }

        var cell = action.Action.Target;

        // Own cell: capture / repair counts as "fighting" in the broad sense; a plain wait does not.
        if (selected.Cell == cell)
            return !IsIdleConfirmOnOwnCell(action);

        // Enemy unit / building / gravestone on the target cell → attack / destroy / raise.
        if (state.TryGetUnitAt(cell, out var target) && target.PlayerIndex != state.CurrentPlayer)
            return true;
        if (state.TryGetBuildingAt(cell, out _))
            return true;
        return state.HasGravestoneAt(cell);
    }

    private static bool IsIdleConfirmOnOwnCell(BotAtomicAction action) =>
        action.Kind == BotAtomicActionKind.ConfirmAt && action.Action.Kind == MatchActionKind.WaitUnit;

    /// <summary>
    /// Negamax-style search with one function for both sides: maximise on the bot's turn,
    /// minimise on the opponent's (scores are always from <paramref name="botPlayerIndex"/>'s view).
    /// </summary>
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
        // Node budget exhausted or match over → static evaluation.
        if (nodes >= profile.NodeLimit || state.IsMatchOver)
            return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);

        var maximizing = state.CurrentPlayer == botPlayerIndex;

        // Depth done and no quiescence pending → leaf.
        if (depthLeft <= 0 && quiescenceLeft <= 0)
            return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);

        var actions = LegalActionGenerator.Generate(state);
        if (actions.Count == 0)
            return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);

        // In the bot's own branches cut pure idling again, so the tree is not "Wait → Wait → …".
        if (maximizing)
            actions = PreferProductive(state, actions, profile);

        if (depthLeft <= 0 && quiescenceLeft > 0)
        {
            // Quiescence: the main depth is spent, but after a noisy move look a few plies further
            // (End turn / Wait / noisy only) so the bot does not score "I hit, +X" and miss the counter.
            actions = actions
                .Where(action =>
                    action.Kind == BotAtomicActionKind.EndTurn
                    || action.Kind == BotAtomicActionKind.WaitSelected
                    || action.IsNoisyForQuiescence)
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

            var child = state.Clone();
            child.TryApply(action.Action);
            nodes++;

            // Spend regular depth first; at zero, tick quiescence down. A noisy move within
            // regular depth may grant QuiescencePlies again.
            var nextDepth = depthLeft > 0 ? depthLeft - 1 : 0;
            var nextQuiescence = depthLeft > 0
                ? (profile.UseQuiescence && action.IsNoisyForQuiescence ? profile.QuiescencePlies : 0)
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

            // α-β cut-off: the window closed; remaining moves cannot change the parent's choice.
            if (alpha >= beta)
                break;
        }

        return best;
    }
}
