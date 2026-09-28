using TinyTBS.Game.Ai;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Match;

/// <summary>Payload to start a match from the New Game screen.</summary>
public sealed class NewGameStartRequest
{
    public required string ScenarioModuleId { get; init; }

    public required string LevelId { get; init; }

    /// <summary>
    /// When null, the load pipeline uses <c>scenario.defaults</c>.
    /// Bundle / scenario-defaults picker supplies an override from New Game.
    /// </summary>
    public MatchContentComposition? Composition { get; init; }

    /// <summary>When null, the pipeline uses the level's <c>players.defaultSlots</c>.</summary>
    public int? PlayerCount { get; init; }

    /// <summary>When null, the pipeline uses the level's <c>defaultStartingGold</c>.</summary>
    public int? StartingGold { get; init; }

    /// <summary>When null, the pipeline uses the level's <c>defaultUnitCap</c>.</summary>
    public int? UnitCap { get; init; }

    /// <summary>
    /// Per-slot controllers. When null or shorter than player count, missing seats default to Local.
    /// </summary>
    public IReadOnlyList<MatchPlayerSeat>? PlayerSeats { get; init; }

    /// <summary>When set, attach campaign run state after the session is built.</summary>
    public CampaignRunState? CampaignRun { get; init; }
}
