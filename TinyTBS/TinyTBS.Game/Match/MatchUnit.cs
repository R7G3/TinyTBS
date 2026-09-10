namespace TinyTBS.Game.Match;

/// <summary>Logical unit on the match grid (no rendering).</summary>
public sealed class MatchUnit
{
    public MatchUnit(int id, GridCell cell, int playerIndex)
    {
        Id = id;
        Cell = cell;
        PlayerIndex = playerIndex;
    }

    public int Id { get; }

    public GridCell Cell { get; set; }

    public int PlayerIndex { get; }
}
