using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Gravestone on a cell without a building. Expires after the dead owner's next turn (inclusive).
/// Map-placed stones use <see cref="SourcePlayerIndex"/> = -1 and never auto-expire.
/// </summary>
public sealed class MatchGravestone
{
    public MatchGravestone(GridCell cell, int sourcePlayerIndex, int expiresWhenTurnStartsReaches)
    {
        Cell = cell;
        SourcePlayerIndex = sourcePlayerIndex;
        ExpiresWhenTurnStartsReaches = expiresWhenTurnStartsReaches;
    }

    public GridCell Cell { get; set; }

    /// <summary>Player whose unit died; -1 for map-placed stones.</summary>
    public int SourcePlayerIndex { get; }

    /// <summary>
    /// Removed at the start of that player's turn when
    /// <c>_turnStartsByPlayer[Source]</c> reaches this value (deathTurnStarts + 2).
    /// </summary>
    public int ExpiresWhenTurnStartsReaches { get; }

    public MatchGravestone Clone() => new(Cell, SourcePlayerIndex, ExpiresWhenTurnStartsReaches);
}
