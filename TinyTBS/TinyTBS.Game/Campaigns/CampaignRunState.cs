using TinyTBS.Game.Ai;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Saves;
using TinyTBS.Game.Saves.Models;

namespace TinyTBS.Game.Campaigns;

/// <summary>In-memory campaign run attached to a live match session.</summary>
public sealed class CampaignRunState
{
    public required string CampaignId { get; init; }

    public required string ScenarioModuleId { get; init; }

    public required string CampaignTitle { get; init; }

    public required string CurrentLevelId { get; set; }

    public required List<string> UnlockedLevelIds { get; init; }

    public string? PendingNextLevelId { get; set; }

    public required Dictionary<string, string> Extensions { get; init; }

    public MatchContentComposition? Composition { get; set; }

    public IReadOnlyList<MatchPlayerSeat>? PlayerSeats { get; set; }

    public int? UnitCap { get; set; }

    public string? ProgressFilePath { get; set; }

    /// <summary>
    /// Set when a campaign hook fails; later hooks in this run are skipped so one bad script
    /// cannot keep throwing on every chapter transition.
    /// </summary>
    public string? ScriptFailureMessage { get; set; }

    public static CampaignRunState FromProgress(
        CampaignProgressDocument progress,
        MatchContentComposition? composition,
        IReadOnlyList<MatchPlayerSeat>? seats)
    {
        ArgumentNullException.ThrowIfNull(progress);
        return new CampaignRunState
        {
            CampaignId = progress.CampaignId,
            ScenarioModuleId = progress.ScenarioModuleId,
            CampaignTitle = progress.CampaignTitle ?? progress.CampaignId,
            CurrentLevelId = progress.CurrentLevelId,
            UnlockedLevelIds = progress.UnlockedLevelIds.ToList(),
            PendingNextLevelId = progress.PendingNextLevelId,
            Extensions = new Dictionary<string, string>(progress.Extensions, StringComparer.Ordinal),
            Composition = composition,
            PlayerSeats = seats,
            UnitCap = progress.UnitCap,
        };
    }

    public CampaignProgressDocument ToProgressDocument(MatchSaveContentSetup? contentSetup)
    {
        return new CampaignProgressDocument
        {
            SaveVersion = CampaignProgressDocument.CurrentSaveVersion,
            Kind = CampaignProgressDocument.KindCampaign,
            WrittenAtUtc = DateTimeOffset.UtcNow,
            CampaignId = CampaignId,
            ScenarioModuleId = ScenarioModuleId,
            CampaignTitle = CampaignTitle,
            CurrentLevelId = CurrentLevelId,
            UnlockedLevelIds = UnlockedLevelIds.ToList(),
            PendingNextLevelId = PendingNextLevelId,
            UnitCap = UnitCap,
            ContentSetup = contentSetup,
            PlayerSeats = PlayerSeats?.Select(MatchSaveSeatCodec.ToSave).ToList() ?? [],
            Extensions = new Dictionary<string, string>(Extensions, StringComparer.Ordinal),
        };
    }
}
