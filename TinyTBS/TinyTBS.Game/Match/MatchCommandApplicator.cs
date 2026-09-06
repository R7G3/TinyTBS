using TinyTBS.Core.Input;
using TinyTBS.Core.Match;

namespace TinyTBS.Game.Match;

/// <summary>
/// Applies logical game commands to a match session (no devices, no pixels).
/// </summary>
public static class MatchCommandApplicator
{
    /// <summary>
    /// Returns true when the player requested leaving the match (Back).
    /// </summary>
    public static bool Apply(MatchSession match, IGameCommandSource commands)
    {
        if (commands.WasPressed(GameCommand.Back))
            return true;

        if (commands.WasPressed(GameCommand.EndTurn))
            match.EndTurn();

        if (commands.WasPressed(GameCommand.Confirm))
            match.HandleConfirm();

        if (commands.WasPressed(GameCommand.NavigateUp))
            match.MoveCursor(0, -1);
        if (commands.WasPressed(GameCommand.NavigateDown))
            match.MoveCursor(0, 1);
        if (commands.WasPressed(GameCommand.NavigateLeft))
            match.MoveCursor(-1, 0);
        if (commands.WasPressed(GameCommand.NavigateRight))
            match.MoveCursor(1, 0);

        return false;
    }

    public static void ApplyPointer(MatchSession match, GridCell cell) =>
        match.HandlePointer(cell);
}
