namespace TinyTBS.Game.Match;

/// <summary>Logical unit on the match grid (no rendering).</summary>
public sealed class MatchUnit
{
    public MatchUnit(int id, UnitKind kind, GridCell cell, int playerIndex)
    {
        Id = id;
        Kind = kind;
        Cell = cell;
        PlayerIndex = playerIndex;
    }

    public int Id { get; }

    public UnitKind Kind { get; }

    public GridCell Cell { get; set; }

    public int PlayerIndex { get; }
}
