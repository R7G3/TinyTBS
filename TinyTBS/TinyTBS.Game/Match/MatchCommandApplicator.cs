using TinyTBS.Engine.Input;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Match;

/// <summary>
/// Applies logical game commands / pointer picks to a <see cref="GameplaySession"/> (no devices, no draw).
/// </summary>
public static class MatchCommandApplicator
{
    /// <summary>
    /// Returns true when the player requested leaving the match (Back).
    /// </summary>
    public static bool Apply(GameplaySession session, IGameCommandSource commands)
    {
        if (commands.WasPressed(GameCommand.Back))
            return true;

        if (commands.WasPressed(GameCommand.EndTurn))
            session.EndTurn();

        if (commands.WasPressed(GameCommand.Confirm))
            session.Confirm();

        var match = session.State;
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

        if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            match.HandlePointer(new GridCell(cellX, cellY));
    }
}
