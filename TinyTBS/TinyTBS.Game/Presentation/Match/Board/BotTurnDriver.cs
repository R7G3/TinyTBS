using Microsoft.Xna.Framework;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Rules.Ai;
using TinyTBS.Rules.Match;
using TinyTBS.Game.Match.Session;

namespace TinyTBS.Game.Presentation.Match.Board;

/// <summary>
/// Plays a bot seat in a live match the way a person would: decide, slide the board cursor to the target
/// cell, then act — so selections, walks and strikes read as a player at the controls rather than instant
/// teleports. The search runs on a background task over a cloned state, so frames keep flowing while the
/// bot thinks. At most one step (aim or apply) happens per frame, with a short pause after each action.
/// </summary>
public sealed class BotTurnDriver
{
    private readonly IBotSearchPolicy _search;
    private readonly TimeSpan _minActionInterval;
    private TimeSpan _cooldownRemaining;
    private BotAtomicAction? _pendingAction;
    private Task<BotAtomicAction>? _searchTask;
    private int _searchPlayerIndex = -1;

    public BotTurnDriver(IBotSearchPolicy? search = null, TimeSpan? minActionInterval = null)
    {
        _search = search ?? new AtomicAlphaBetaSearch();
        // Cursor travel already gives a human rhythm, so the pause between actions stays short.
        _minActionInterval = minActionInterval ?? TimeSpan.FromMilliseconds(160);
    }

    /// <summary>
    /// Advances the bot by one step when it is a bot's turn and the pause has passed.
    /// Returns true when an action was applied to the session.
    /// </summary>
    public bool TryStep(GameplaySession session, GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(session);

        _cooldownRemaining -= gameTime.ElapsedGameTime;
        if (_cooldownRemaining > TimeSpan.Zero)
            return false;

        var match = session.State;
        if (match.IsMatchOver)
            return false;

        var playerIndex = match.CurrentPlayer;
        var seats = session.Runtime.PlayerSeats;
        if (playerIndex < 0 || playerIndex >= seats.Count)
            return false;

        var seat = seats[playerIndex];
        if (seat.Kind != MatchPlayerKind.Bot || match.IsPlayerEliminated(playerIndex))
        {
            Reset(session);
            return false;
        }

        // While the cursor slides, keep the logical cursor on the cell under it (status bar / highlight).
        if (session.Scene.IsCursorAnimating)
        {
            if (session.Scene.TryGetCursorAimLogicalCell(out var aimCell) && aimCell != session.Cursor.Cell)
                session.Cursor.MoveTo(aimCell);
            return false;
        }

        // The cursor arrived (or no aim was needed): act.
        if (_pendingAction is { } pending)
        {
            _pendingAction = null;
            Apply(session, pending);
            _cooldownRemaining = _minActionInterval;
            return true;
        }

        var action = TakeSearchResult(match, playerIndex, seat.BotDifficulty);
        if (action is null)
            return false;

        if (action.AimCell is { } target && session.Scene.BeginCursorAim(session.Cursor.Cell, target))
        {
            _pendingAction = action;
            return false;
        }

        Apply(session, action);
        _cooldownRemaining = _minActionInterval;
        return true;
    }

    /// <summary>Starts a search on the first call; returns its decision once the background task finishes.</summary>
    private BotAtomicAction? TakeSearchResult(MatchState match, int playerIndex, BotDifficulty difficulty)
    {
        if (_searchTask is null || _searchPlayerIndex != playerIndex)
        {
            var snapshot = match.Clone();
            var profile = BotDifficultyProfile.For(difficulty);
            var search = _search;
            _searchPlayerIndex = playerIndex;
            _searchTask = Task.Run(() => search.ChooseAction(snapshot, playerIndex, profile));
            return null;
        }

        if (!_searchTask.IsCompleted)
            return null;

        var task = _searchTask;
        _searchTask = null;
        if (!task.IsFaulted)
            return task.Result;

        GameLog.Error("Bot search failed; ending the bot's turn.", task.Exception?.GetBaseException());
        return new BotAtomicAction { Kind = BotAtomicActionKind.EndTurn, Action = MatchAction.EndTurn };
    }

    private static void Apply(GameplaySession session, BotAtomicAction action)
    {
        // The "click" lands where the cursor is, exactly like a human's.
        if (action.AimCell is { } cell)
            session.Cursor.MoveTo(cell);

        if (session.Runtime.TryApply(action.Action) || action.Kind == BotAtomicActionKind.EndTurn)
            return;

        // A rejected decision would be chosen again every frame; end the turn instead of stalling the match.
        GameLog.Warning($"Bot action rejected by the rules ({action.Action}); ending the bot's turn.");
        session.Runtime.EndTurn();
    }

    private void Reset(GameplaySession session)
    {
        _pendingAction = null;
        _searchTask = null;
        _searchPlayerIndex = -1;
        session.Scene.CancelCursorAim();
    }
}
