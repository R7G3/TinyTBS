namespace TinyTBS.Game.Match;

/// <summary>Snapshot of the last successful player action in the match.</summary>
public sealed class MatchPlayerAction
{
    public required MatchPlayerActionKind Kind { get; init; }
    public required int PlayerIndex { get; init; }
    public int? UnitId { get; init; }
    public GridCell? Source { get; init; }
    public GridCell? Target { get; init; }
}
