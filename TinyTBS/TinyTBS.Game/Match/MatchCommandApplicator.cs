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
    /// Applies board commands when the match is not blocked by pause/shop UI.
    /// Leave-to-menu is only via the pause menu item, not a board command.
    /// </summary>
    public static void Apply(
        GameplaySession session,
        IGameCommandSource commands,
        bool boardInputEnabled,
        bool allowConfirm = true)
    {
        if (!boardInputEnabled)
            return;

        if (commands.WasPressed(GameCommand.EndTurn))
            session.EndTurn();

        if (allowConfirm && commands.WasPressed(GameCommand.Confirm))
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
    }

    /// <summary>
    /// Primary press on a cell: move cursor and confirm (select/move/shop). Hold-drag only moves cursor.
    /// </summary>
    public static void ApplyPointer(
        GameplaySession session,
        IPointerSource pointer,
        MatchBoardLayout layout,
        bool allowConfirm = true)
    {
        var match = session.State;

        if (pointer.WasPrimaryPressed)
        {
            if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            {
                match.HandlePointer(new GridCell(cellX, cellY));
                if (allowConfirm)
                    session.Confirm();
            }

            return;
        }

        if (!pointer.IsPrimaryDown)
            return;

        if (layout.TryScreenToCell(pointer.Position, out var dragX, out var dragY))
            match.HandlePointer(new GridCell(dragX, dragY));
    }

    /// <summary>
    /// Moves the cursor under the secondary press (right mouse). Returns true when that frame had a secondary press.
    /// </summary>
    public static bool TryApplySecondaryPointer(
        MatchState match,
        IPointerSource pointer,
        MatchBoardLayout layout)
    {
        if (!pointer.WasSecondaryPressed)
            return false;

        if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            match.HandlePointer(new GridCell(cellX, cellY));

        return true;
    }
}
