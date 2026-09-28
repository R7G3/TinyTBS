using Microsoft.Xna.Framework;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ai;

/// <summary>
/// Драйвер хода бота в живом матче: раз в ~280 мс выбирает одно атомарное действие
/// и прогоняет его через те же API сессии, что и человек (Confirm / Wait / Recruit / EndTurn).
/// За кадр — не больше одного действия (чтобы UI/анимации успевали).
/// </summary>
public sealed class BotTurnDriver
{
    private readonly IBotSearchPolicy _search;
    private readonly TimeSpan _minActionInterval;
    private TimeSpan _cooldownRemaining;

    public BotTurnDriver(IBotSearchPolicy? search = null, TimeSpan? minActionInterval = null)
    {
        _search = search ?? new AtomicAlphaBetaSearch();
        _minActionInterval = minActionInterval ?? TimeSpan.FromMilliseconds(280);
    }

    /// <summary>
    /// Если сейчас ход бота и кулдаун прошёл — выбрать и применить одно действие.
    /// true = что-то сделали (контроллер может обновить HUD).
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
            return false;

        if (match.IsPlayerEliminated(playerIndex))
            return false;

        // Профиль Easy/Normal → лимиты поиска и веса оценки.
        var profile = BotDifficultyProfile.For(seat.BotDifficulty);
        var action = _search.ChooseAction(match, playerIndex, profile);
        ApplyThroughSession(session, action);
        _cooldownRemaining = _minActionInterval;
        return true;
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
                session.TryWaitSelectedUnit();
                break;
            case BotAtomicActionKind.Recruit:
                session.TryBuyShopOffer(action.UnitTypeId, action.RecruitCost, action.Cell);
                break;
            case BotAtomicActionKind.SelectUnit:
            case BotAtomicActionKind.ConfirmAt:
                // Как клик мышью: курсор на клетку → Confirm.
                match.HandlePointer(action.Cell);
                session.Confirm();
                break;
        }
    }
}
