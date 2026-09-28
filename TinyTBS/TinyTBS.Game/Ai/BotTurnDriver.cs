using Microsoft.Xna.Framework;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ai;

/// <summary>
/// Драйвер хода бота в живом матче: выбирает атомарное действие, плавно ведёт курсор
/// к целевой клетке (как человек), затем Confirm / Wait / Recruit / EndTurn.
/// За кадр — не больше одного «шага» машины состояний (aim или apply).
/// </summary>
public sealed class BotTurnDriver
{
    private readonly IBotSearchPolicy _search;
    private readonly TimeSpan _minActionInterval;
    private TimeSpan _cooldownRemaining;
    private BotAtomicAction? _pendingAction;

    public BotTurnDriver(IBotSearchPolicy? search = null, TimeSpan? minActionInterval = null)
    {
        _search = search ?? new AtomicAlphaBetaSearch();
        // Короче пауза: время на ведение курсора уже даёт «человеческий» ритм.
        _minActionInterval = minActionInterval ?? TimeSpan.FromMilliseconds(160);
    }

    /// <summary>
    /// Если сейчас ход бота и кулдаун прошёл — либо начать/дождаться aim курсора,
    /// либо применить отложенное действие. true = применили действие к сессии.
    /// </summary>
    public bool TryStep(
        GameplaySession session,
        IReadOnlyList<MatchPlayerSeat> seats,
        GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(seats);

        _cooldownRemaining -= gameTime.ElapsedGameTime;
        if (_cooldownRemaining > TimeSpan.Zero)
            return false;

        var match = session.State;
        if (match.IsMatchOver)
            return false;

        var playerIndex = match.CurrentPlayer;
        if (playerIndex < 0 || playerIndex >= seats.Count)
            return false;

        var seat = seats[playerIndex];
        if (seat.Kind != MatchPlayerKind.Bot)
        {
            ClearPending(session);
            return false;
        }

        if (match.IsPlayerEliminated(playerIndex))
        {
            ClearPending(session);
            return false;
        }

        // Пока курсор едет — подтягиваем логический Cursor (статус-бар / подсветка).
        if (session.Scene.IsCursorAnimating)
        {
            if (session.Scene.TryGetCursorAimLogicalCell(out var aimCell)
                && aimCell != match.Cursor)
            {
                match.HandlePointer(aimCell);
            }

            return false;
        }

        // Курсор доехал (или aim не нужен) — выполнить отложенное действие.
        if (_pendingAction is { } pending)
        {
            _pendingAction = null;
            ApplyThroughSession(session, pending);
            _cooldownRemaining = _minActionInterval;
            return true;
        }

        var profile = BotDifficultyProfile.For(seat.BotDifficulty);
        var action = _search.ChooseAction(match, playerIndex, profile);

        if (TryBeginAim(session, action))
        {
            _pendingAction = action;
            return false;
        }

        ApplyThroughSession(session, action);
        _cooldownRemaining = _minActionInterval;
        return true;
    }

    /// <summary>
    /// true = начали анимацию курсора к клетке действия; false = aim не нужен / уже на месте.
    /// </summary>
    private static bool TryBeginAim(GameplaySession session, BotAtomicAction action)
    {
        if (!TryGetAimCell(action, out var target))
            return false;

        var match = session.State;
        return session.Scene.BeginCursorAim(match.Cursor, target);
    }

    private static bool TryGetAimCell(BotAtomicAction action, out GridCell cell)
    {
        switch (action.Kind)
        {
            case BotAtomicActionKind.SelectUnit:
            case BotAtomicActionKind.ConfirmAt:
            case BotAtomicActionKind.Recruit:
            case BotAtomicActionKind.WaitSelected:
                cell = action.Cell;
                return true;
            default:
                cell = default;
                return false;
        }
    }

    private void ClearPending(GameplaySession session)
    {
        _pendingAction = null;
        session.Scene.CancelCursorAim();
    }

    /// <summary>
    /// Те же вызовы, что у игрока: иначе скрипты карты / экономика не увидят действие.
    /// </summary>
    private static void ApplyThroughSession(GameplaySession session, BotAtomicAction action)
    {
        var match = session.State;
        switch (action.Kind)
        {
            case BotAtomicActionKind.EndTurn:
                session.EndTurn();
                break;
            case BotAtomicActionKind.WaitSelected:
                match.HandlePointer(action.Cell);
                session.TryWaitSelectedUnit();
                break;
            case BotAtomicActionKind.Recruit:
                match.HandlePointer(action.Cell);
                session.TryBuyShopOffer(action.UnitTypeId, action.RecruitCost, action.Cell);
                break;
            case BotAtomicActionKind.SelectUnit:
            case BotAtomicActionKind.ConfirmAt:
                match.HandlePointer(action.Cell);
                session.Confirm();
                break;
        }
    }
}
