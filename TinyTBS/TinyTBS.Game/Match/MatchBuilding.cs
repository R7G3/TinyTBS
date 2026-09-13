namespace TinyTBS.Game.Match;

/// <summary>Logical building on the match grid (no rendering).</summary>
public sealed class MatchBuilding
{
    public MatchBuilding(BuildingKind kind, GridCell cell, int? ownerPlayerIndex)
    {
        Kind = kind;
        Cell = cell;
        OwnerPlayerIndex = ownerPlayerIndex;
    }

    public BuildingKind Kind { get; }

    public GridCell Cell { get; }

    /// <summary>Null = neutral (unowned).</summary>
    public int? OwnerPlayerIndex { get; }
}
