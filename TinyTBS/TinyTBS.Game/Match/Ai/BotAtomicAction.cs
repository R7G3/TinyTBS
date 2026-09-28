using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match.Ai;

/// <summary>
/// One atomic action for search / apply. Depth in αβ counts these nodes —
/// not full player turns (full-turn Hard can be a separate <see cref="IBotSearchPolicy"/> later).
/// </summary>
public sealed class BotAtomicAction
{
    public required BotAtomicActionKind Kind { get; init; }

    public int UnitId { get; init; }

    public GridCell Cell { get; init; }

    public ContentId UnitTypeId { get; init; }

    public int RecruitCost { get; init; }

    /// <summary>Stable order key for deterministic tie-breaks.</summary>
    public int TieBreak { get; init; }
}
