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
        var nodes = 0; // сколько узлов дерева уже раскрыли (лимит NodeLimit)
        BotAtomicAction? bestAction = null;
        var bestScore = int.MinValue;

        // На корне перебираем каждый кандидат: применили → смотрим, чем ответит дерево ниже.
        foreach (var action in candidates.OrderBy(a => a.TieBreak))
        {
            if (nodes >= profile.NodeLimit)
                break;

            // Клон доски: поиск не должен портить живой матч.
            var child = state.CloneForAi();
            BotAtomicActionApplicator.Apply(child, action);
            nodes++;

            // depthLeft: сколько ещё «обычных» полуходов (ply) можно углубиться.
            //   MaxDepth=2 → после корневого действия остаётся 1 ply в дереве.
            // quiescenceLeft: доп. глубина только для «шумных» ходов (удар/найм),
            //   чтобы не оценивать позицию сразу после атаки, не досмотрев ответ.
            // alpha / beta: окно отсечения α-β —
            //   alpha = лучшее, что максимизатор уже гарантировал себе;
            //   beta  = лучшее (наименьшее), что минимизатор уже гарантировал.
            //   Если alpha >= beta, ветка бесполезна — дальше не смотрим (cut-off).
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

            // На корне бот всегда максимизирует свою оценку; при равенстве — меньший TieBreak.
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
