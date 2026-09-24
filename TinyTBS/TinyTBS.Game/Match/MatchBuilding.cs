namespace TinyTBS.Game.Match;

/// <summary>Logical building on the match grid (no rendering).</summary>
public sealed class MatchBuilding
{
    public MatchBuilding(BuildingKind kind, GridCell cell, int? ownerPlayerIndex, bool isRuined = false)
    {
        Kind = kind;
        Cell = cell;
        OwnerPlayerIndex = ownerPlayerIndex;
        IsRuined = isRuined;
    }

    public BuildingKind Kind { get; }

    public GridCell Cell { get; }

    /// <summary>Null = neutral (unowned).</summary>
    public int? OwnerPlayerIndex { get; }

    /// <summary>True when map/state is <c>ruined</c> (e.g. destroyed village).</summary>
    public bool IsRuined { get; }
}
