using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Rules.Modules.Models;
using TinyTBS.Rules.Saves.Models;

namespace TinyTBS.Game.Campaigns;

/// <summary>Creates and advances campaign progress documents.</summary>
public static class CampaignProgressFactory
{
    public static CampaignProgressDocument CreateNew(
        CampaignDefinition campaign,
        string scenarioModuleId,
        MatchContentComposition composition,
        IReadOnlyList<MatchSaveSeat> playerSeats,
        int? unitCap,
        IReadOnlyDictionary<string, string>? moduleVersions = null)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleId);
        ArgumentNullException.ThrowIfNull(composition);

        var firstLevelId = campaign.Chapters[0].LevelId;
        return new CampaignProgressDocument
        {
            SaveVersion = CampaignProgressDocument.CurrentSaveVersion,
            Kind = CampaignProgressDocument.KindCampaign,
            WrittenAtUtc = DateTimeOffset.UtcNow,
            CampaignId = campaign.CampaignId,
            ScenarioModuleId = scenarioModuleId.Trim(),
            CampaignTitle = campaign.Title,
            CurrentLevelId = firstLevelId,
            UnlockedLevelIds = [firstLevelId],
            PendingNextLevelId = null,
            UnitCap = unitCap,
            ContentSetup = ToContentSetup(composition, moduleVersions),
            PlayerSeats = playerSeats.ToList(),
            Extensions = new Dictionary<string, string>(StringComparer.Ordinal),
        };
    }

    /// <summary>
    /// Existing progress moved to <paramref name="levelId"/> (an unlocked chapter picked in New Game),
    /// with the lobby's content, seats and unit cap.
    /// </summary>
    public static CampaignProgressDocument AtChapter(
        CampaignProgressDocument existing,
        CampaignDefinition campaign,
        string levelId,
        MatchContentComposition composition,
        IReadOnlyList<MatchSaveSeat> playerSeats,
        int? unitCap)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentException.ThrowIfNullOrWhiteSpace(levelId);
        ArgumentNullException.ThrowIfNull(composition);

        var unlocked = existing.UnlockedLevelIds.ToList();
        if (!unlocked.Contains(levelId, StringComparer.Ordinal))
            unlocked.Add(levelId);

        return new CampaignProgressDocument
        {
            SaveVersion = existing.SaveVersion,
            Kind = CampaignProgressDocument.KindCampaign,
            WrittenAtUtc = DateTimeOffset.UtcNow,
            CampaignId = existing.CampaignId,
            ScenarioModuleId = existing.ScenarioModuleId,
            CampaignTitle = existing.CampaignTitle ?? campaign.Title,
            CurrentLevelId = levelId,
            UnlockedLevelIds = unlocked,
            PendingNextLevelId = existing.PendingNextLevelId,
            UnitCap = unitCap,
            ContentSetup = ToContentSetup(composition, moduleVersions: null),
            PlayerSeats = playerSeats.ToList(),
            Extensions = new Dictionary<string, string>(existing.Extensions, StringComparer.Ordinal),
        };
    }

    public static MatchSaveContentSetup ToContentSetup(
        MatchContentComposition composition,
        IReadOnlyDictionary<string, string>? moduleVersions)
    {
        return new MatchSaveContentSetup
        {
            ScenarioModuleId = composition.ScenarioModuleId,
            UnitsModuleIds = composition.UnitsModuleIds.ToList(),
            BuildingsModuleIds = composition.BuildingsModuleIds.ToList(),
            ThemeModuleId = composition.ThemeModuleId,
            ModuleVersions = moduleVersions is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(moduleVersions, StringComparer.Ordinal),
            Replaces = composition.Replaces
                .Select(replace => new MatchSaveReplace
                {
                    From = replace.From.Full,
                    To = replace.To.Full,
                })
                .ToList(),
        };
    }

    public static HashSet<string> BuildUnlockedSet(CampaignProgressDocument? progress, CampaignDefinition campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        if (progress is null
            || !string.Equals(progress.CampaignId, campaign.CampaignId, StringComparison.Ordinal))
        {
            return new HashSet<string>([campaign.Chapters[0].LevelId], StringComparer.Ordinal);
        }

        return new HashSet<string>(progress.UnlockedLevelIds, StringComparer.Ordinal);
    }

    public static string PreferPlayableLevelId(
        CampaignDefinition campaign,
        CampaignProgressDocument? progress)
    {
        if (progress is not null
            && string.Equals(progress.CampaignId, campaign.CampaignId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(progress.CurrentLevelId))
        {
            return progress.CurrentLevelId;
        }

        return campaign.Chapters[0].LevelId;
    }
}
