using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ai;

/// <summary>
/// Поиск лучшего атомарного хода бота (α-β / negamax).
/// Глубина считается в атомарных действиях (выбрать юнита, шагнуть, ударить…),
/// а не в полных ходах игрока. Hard позже может заменить политику, не трогая этот класс.
/// </summary>
public sealed class AtomicAlphaBetaSearch : IBotSearchPolicy
{
    public BotAtomicAction ChooseAction(MatchState state, int botPlayerIndex, BotDifficultyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(profile);

        // Все легальные «кнопки» в текущей позиции (как у человека: Select / Confirm / Wait / Recruit / EndTurn).
        var rootActions = LegalActionGenerator.Generate(state);
        if (rootActions.Count == 0)
        {
            return new BotAtomicAction
            {
                Kind = BotAtomicActionKind.EndTurn,
                TieBreak = 0,
            };
        }

        // Не даём боту «стоять», пока есть нормальные ходы (атака/ход/найм).
        // Easy: Wait всё же допускается, если остались только перестановки без боя.
        var candidates = PreferProductive(state, rootActions, profile);
        var nodes = 0;
        BotAtomicAction? bestAction = null;
        var bestScore = int.MinValue;
        var bestCastleDist = int.MaxValue;
        var bestSafety = int.MaxValue;
        var opponent = PositionEvaluator.OpponentIndex(state, botPlayerIndex);
        var rootSafety = PositionEvaluator.MeasureKingSafetyPenalty(
            state,
            botPlayerIndex,
            opponent,
            profile.KingSafetyWeight);

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

            var childCastleDist = PositionEvaluator.MeasureKingEnemyCastleDistance(child, botPlayerIndex);
            var childSafety = PositionEvaluator.MeasureKingSafetyPenalty(
                child,
                botPlayerIndex,
                opponent,
                profile.KingSafetyWeight);

            if (bestAction is null
                || IsBetterRootCandidate(
                    score,
                    childSafety,
                    childCastleDist,
                    action.TieBreak,
                    bestScore,
                    bestSafety,
                    bestCastleDist,
                    bestAction.TieBreak,
                    rootSafety))
            {
                bestScore = score;
                bestAction = action;
                bestCastleDist = childCastleDist;
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
        int childCastleDist,
        int childTieBreak,
        int bestScore,
        int bestSafety,
        int bestCastleDist,
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
            if (childCastleDist != bestCastleDist)
                return childCastleDist < bestCastleDist;
            return childTieBreak < bestTieBreak;
        }

        if (childSafety != bestSafety)
            return childSafety < bestSafety;
        return childTieBreak < bestTieBreak;
    }

    /// <summary>
    /// Оставляет только «полезные» действия, если они есть; иначе возвращает исходный список
    /// (например, когда остались только Wait / EndTurn).
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
                // Confirm на своей клетке без захвата = «встать» — не считаем продуктивным.
                return !IsIdleConfirmOnOwnCell(state, action);
            case BotAtomicActionKind.WaitSelected:
                // Easy может выбрать Wait вместо длинного марша, если нет боя/найма.
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

        // Своя клетка: захват/ремонт — «бой» в широком смысле; пустой Wait — нет.
        if (selected.Cell == action.Cell)
            return !IsIdleConfirmOnOwnCell(state, action);

        // Чужой юнит / здание / надгробие на клетке Confirm → атака / destroy / raise.
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

        // Confirm на своей клетке без здания → FinishUnitActivation(Wait).
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

    /// <summary>
    /// Negamax: одна функция и для «нашего» и для «чужого» хода.
    /// Когда ход бота — максимизируем оценку; когда ход оппонента — минимизируем
    /// (оценка всегда с точки зрения <paramref name="botPlayerIndex"/>).
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
        // Бюджет узлов / конец матча → статическая оценка позиции.
        if (nodes >= profile.NodeLimit || state.IsMatchOver)
            return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);

        // maximizing == true: сейчас ходит наш бот (хотим высокий score).
        // maximizing == false: ходит соперник (хотим, чтобы его лучший ответ дал нам низкий score).
        var maximizing = state.CurrentPlayer == botPlayerIndex;

        // Глубина кончилась и quiescence не активен → лист дерева, только Evaluate.
        if (depthLeft <= 0 && quiescenceLeft <= 0)
            return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);

        var actions = LegalActionGenerator.Generate(state);
        if (actions.Count == 0)
            return PositionEvaluator.Evaluate(state, botPlayerIndex, profile);

        // В ветках бота снова режем чистый простой, чтобы дерево не «Wait → Wait → …».
        if (maximizing)
            actions = PreferProductive(state, actions, profile);

        if (depthLeft <= 0 && quiescenceLeft > 0)
        {
            // Режим quiescence: основная глубина исчерпана, но после «шумного» хода
            // смотрим ещё несколько ply — только EndTurn / Wait / Confirm / Recruit.
            // Иначе бот мог бы оценить позицию «я ударил, очки +X», не учтя контрудар.
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

        // Стартовое «лучшее»: для макса — очень плохо, для мина — очень хорошо.
        var best = maximizing ? int.MinValue + 1 : int.MaxValue - 1;

        foreach (var action in actions)
        {
            if (nodes >= profile.NodeLimit)
                break;

            var child = state.CloneForAi();
            BotAtomicActionApplicator.Apply(child, action);
            nodes++;

            // Спуск: сначала тратим обычную глубину; когда она 0 — тикаем quiescence.
            // Если ещё в обычной глубине и ход «шумный» — можем заново выдать QuiescencePlies.
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
                // Берём максимум; поднимаем alpha (гарантия максимизатора).
                if (score > best)
                    best = score;
                if (best > alpha)
                    alpha = best;
            }
            else
            {
                // Берём минимум; опускаем beta (гарантия минимизатора).
                if (score < best)
                    best = score;
                if (best < beta)
                    beta = best;
            }

            // α-β отсечение: окно схлопнулось — остальные ходы не изменят выбор предка.
            if (alpha >= beta)
                break;
        }

        return best;
    }
}
