namespace TinyTBS.Game.Match;

/// <summary>
/// Read-only action overlay for a unit (own selection or enemy threat hold).
/// Does not mutate match state.
/// </summary>
public sealed class MatchUnitActionOverlay
{
    public required IReadOnlyList<GridCell> MoveCells { get; init; }

    public required IReadOnlyList<GridCell> AttackCells { get; init; }

    public required IReadOnlyList<GridCell> CaptureCells { get; init; }

    public required IReadOnlyList<GridCell> RepairCells { get; init; }

    public required IReadOnlyList<GridCell> RaiseCells { get; init; }
}
