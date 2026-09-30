using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Saves;
using TinyTBS.Rules.Saves.Models;

namespace TinyTBS.Game.Campaigns;

/// <summary>Restores <see cref="CampaignRunState"/> from progress or an orphan match link.</summary>
public static class CampaignRunRestorer
{
    public static CampaignRunState? TryRestoreForMatch(
        MatchSaveDocument document,
        IFileSystem files,
        IUserDataPaths userDataPaths)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (string.IsNullOrWhiteSpace(document.CampaignId))
            return null;

        var campaignId = document.CampaignId;
        var scenarioModuleId = document.ContentSetup.ScenarioModuleId;
        var levelId = string.IsNullOrWhiteSpace(document.CampaignLevelId)
            ? document.LevelId
            : document.CampaignLevelId;

        var progressStore = new CampaignProgressStore(files, userDataPaths);
        var progress = progressStore.TryLoadLatestForCampaign(scenarioModuleId, campaignId);
        if (progress is null)
        {
            // Orphan match: recreate a minimal progress shell; full write happens on chapter end.
            progress = new CampaignProgressDocument
            {
                SaveVersion = CampaignProgressDocument.CurrentSaveVersion,
                Kind = CampaignProgressDocument.KindCampaign,
                WrittenAtUtc = DateTimeOffset.UtcNow,
                CampaignId = campaignId,
                ScenarioModuleId = scenarioModuleId,
                CampaignTitle = campaignId,
                CurrentLevelId = levelId,
                UnlockedLevelIds = [levelId],
                ContentSetup = document.ContentSetup,
                PlayerSeats = document.PlayerSeats.ToList(),
                UnitCap = document.UnitCap,
                Extensions = new Dictionary<string, string>(
                    document.Extensions ?? new Dictionary<string, string>(),
                    StringComparer.Ordinal),
            };
        }

        var composition = MatchSaveDocumentFactory.ToComposition(document.ContentSetup);
        var seats = MatchSaveSeatCodec.FromSaveList(document.PlayerSeats);
        var run = CampaignRunState.FromProgress(progress, composition, seats);
        run.CurrentLevelId = levelId;
        if (!run.UnlockedLevelIds.Contains(levelId, StringComparer.Ordinal))
            run.UnlockedLevelIds.Add(levelId);
        run.ProgressFilePath = progressStore.FindLatestPathForCampaign(scenarioModuleId, campaignId);
        return run;
    }

    public static MatchStartRequest CreateChapterStartRequest(CampaignProgressDocument progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (progress.ContentSetup is null)
            throw new MatchSaveException("Campaign progress is missing contentSetup.");

        var composition = MatchSaveDocumentFactory.ToComposition(progress.ContentSetup);
        var seats = MatchSaveSeatCodec.FromSaveList(progress.PlayerSeats);
        var run = CampaignRunState.FromProgress(progress, composition, seats);

        return new MatchStartRequest
        {
            ScenarioModuleId = progress.ScenarioModuleId,
            LevelId = progress.CurrentLevelId,
            Composition = composition,
            PlayerCount = seats.Count > 0 ? seats.Count : null,
            UnitCap = progress.UnitCap,
            PlayerSeats = seats,
            CampaignRun = run,
        };
    }
}
