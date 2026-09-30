using TinyTBS.Rules.Match;

namespace TinyTBS.Game.Presentation.Match.Board;

/// <summary>Board cursor cell (interaction state, not a match rule). Always clamped to the board.</summary>
public sealed class MatchCursor
{
    private readonly int _width;
    private readonly int _height;

    public MatchCursor(int width, int height, GridCell initial)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        _width = width;
        _height = height;
        MoveTo(initial);
    }

    public GridCell Cell { get; private set; }

    /// <summary>Unit in its activation. The rules receive this on each command and do not store it.</summary>
    public int? SelectedUnitId { get; set; }

    /// <summary>Cursor for a fresh match: the centre of the board.</summary>
    public static MatchCursor AtBoardCentre(int width, int height) =>
        new(width, height, new GridCell(width / 2, height / 2));

    public void MoveBy(int deltaX, int deltaY) => MoveTo(new GridCell(Cell.X + deltaX, Cell.Y + deltaY));

    public void MoveTo(GridCell cell) =>
        Cell = new GridCell(Math.Clamp(cell.X, 0, _width - 1), Math.Clamp(cell.Y, 0, _height - 1));
}
