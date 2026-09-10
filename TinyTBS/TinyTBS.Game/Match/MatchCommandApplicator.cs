using TinyTBS.Engine.Input;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Match;

/// <summary>
/// Applies logical game commands / pointer picks to <see cref="MatchState"/> (no devices, no draw).
/// </summary>
public static class MatchCommandApplicator
{
    /// <summary>
    /// Returns true when the player requested leaving the match (Back).
    /// </summary>
    public static bool Apply(MatchState match, IGameCommandSource commands)
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

    public static void ApplyPointer(MatchState match, IPointerSource pointer, MatchBoardLayout layout)
    {
        if (!pointer.IsPrimaryDown)
            return;

        if (layout.TryScreenToCell(pointer.Position, out var x, out var y))
            match.HandlePointer(new GridCell(x, y));
    }
}
