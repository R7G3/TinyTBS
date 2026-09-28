using Microsoft.Xna.Framework;

namespace TinyTBS.Game.Match.Ai;

/// <summary>
/// Drives one bot seat: repeatedly chooses and applies atomic actions until the
/// current player changes, the match ends, or a per-frame action budget is spent.
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
    /// Applies at most one atomic action when the current player is a bot and cooldown elapsed.
    /// Returns true if an action was applied (session hooks should run for Confirm/EndTurn/Wait/Recruit).
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

        var profile = BotDifficultyProfile.For(seat.BotDifficulty);
        var action = _search.ChooseAction(match, playerIndex, profile);
        ApplyThroughSession(session, action);
        _cooldownRemaining = _minActionInterval;
        return true;
    }

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
                match.HandlePointer(action.Cell);
                session.Confirm();
                break;
        }
    }
}
