using TinyTBS.Rules.Ai;
using TinyTBS.Game.Campaigns;
using TinyTBS.Rules.Modules.Models;
using TinyTBS.Game.Saves;
using TinyTBS.Rules.Saves.Models;

namespace TinyTBS.Game.Match.Session;

/// <summary>
/// Everything needed to build a match session: New Game, campaign chapters, Continue and Load.
/// Null overrides fall back to the level / scenario defaults in <see cref="MatchSessionLoadPipeline"/>.
/// </summary>
public sealed class MatchStartRequest
{
    public required string ScenarioModuleId { get; init; }

    public required string LevelId { get; init; }

    /// <summary>When null, the load pipeline uses <c>scenario.defaults</c>.</summary>
    public MatchContentComposition? Composition { get; init; }

    /// <summary>When null, the pipeline uses the level's <c>players.defaultSlots</c>.</summary>
    public int? PlayerCount { get; init; }

    /// <summary>When null, the pipeline uses the level's <c>defaultStartingGold</c>.</summary>
    public int? StartingGold { get; init; }

    /// <summary>When null, the pipeline uses the level's <c>defaultUnitCap</c>.</summary>
    public int? UnitCap { get; init; }

    /// <summary>Per-slot controllers. Missing seats default to Local.</summary>
    public IReadOnlyList<MatchPlayerSeat>? PlayerSeats { get; init; }

    /// <summary>When set, attach campaign run state after the session is built.</summary>
    public CampaignRunState? CampaignRun { get; init; }

    /// <summary>When set, the match resumes from this saved runtime state instead of starting fresh.</summary>
    public MatchRuntimeSnapshot? ResumeSnapshot { get; init; }

    /// <summary>Optional note shown by the loading screen (for example a module version mismatch).</summary>
    public string? LoadingNote { get; init; }

    public bool IsResume => ResumeSnapshot is not null;

    public static MatchStartRequest FromSaveDocument(
        MatchSaveDocument document,
        CampaignRunState? campaignRun = null,
        string? loadingNote = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new MatchStartRequest
        {
            ScenarioModuleId = document.ContentSetup.ScenarioModuleId,
            LevelId = document.LevelId,
            Composition = MatchSaveDocumentFactory.ToComposition(document.ContentSetup),
            PlayerCount = document.Match.PlayerCount,
            UnitCap = document.Match.UnitCap,
            PlayerSeats = MatchSaveSeatCodec.FromSaveList(document.PlayerSeats),
            ResumeSnapshot = document.Match,
            CampaignRun = campaignRun,
            LoadingNote = loadingNote,
        };
    }
}
