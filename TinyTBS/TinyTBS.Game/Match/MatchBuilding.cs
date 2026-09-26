using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match;

/// <summary>Logical building on the match grid (no rendering).</summary>
public sealed class MatchBuilding
{
    public MatchBuilding(
        ContentId typeId,
        GridCell cell,
        int? ownerPlayerIndex,
        bool isRuined,
        bool allowsRecruit)
    {
        TypeId = typeId;
        Cell = cell;
        OwnerPlayerIndex = ownerPlayerIndex;
        IsRuined = isRuined;
        AllowsRecruit = allowsRecruit;
    }

    public ContentId TypeId { get; }

    public GridCell Cell { get; }

    /// <summary>Null = neutral (unowned).</summary>
    public int? OwnerPlayerIndex { get; }

    /// <summary>True when map/state is <c>ruined</c> (e.g. destroyed village).</summary>
    public bool IsRuined { get; }

    /// <summary>Copied from building definition at spawn (castle / recruit site).</summary>
    public bool AllowsRecruit { get; }
}
