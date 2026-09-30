using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Saves.Models;

namespace TinyTBS.Game.Saves;

/// <summary>One row spanning match and campaign saves.</summary>
public sealed class SaveCatalogEntry
{
    public required string FilePath { get; init; }

    public required DateTimeOffset WrittenAtUtc { get; init; }

    public required string Kind { get; init; }

    public required string Title { get; init; }

    public required string Meta { get; init; }

    public string? CampaignId { get; init; }

    public string? LevelId { get; init; }

    public string? ScenarioModuleId { get; init; }

    public bool IsCampaign =>
        string.Equals(Kind, CampaignProgressDocument.KindCampaign, StringComparison.OrdinalIgnoreCase);

    public bool IsMatch =>
        string.Equals(Kind, MatchSaveDocument.KindMatch, StringComparison.OrdinalIgnoreCase);

    public static SaveCatalogEntry FromMatch(IFileSystem files, MatchSaveListEntry match)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(match);
        string meta;
        string? campaignId = null;
        try
        {
            var document = MatchSaveReader.ReadFile(files, match.FilePath);
            campaignId = document.CampaignId;
            meta = string.IsNullOrWhiteSpace(document.CampaignId)
                ? $"Match · {match.DisplayMeta}"
                : $"Match · Campaign {document.CampaignId} · {document.CampaignLevelId ?? document.LevelId} · {match.WrittenAtUtc.ToUniversalTime():yyyy-MM-dd HH:mm} UTC";
        }
        catch (Exception)
        {
            meta = $"Match · {match.DisplayMeta}";
        }

        return new SaveCatalogEntry
        {
            FilePath = match.FilePath,
            WrittenAtUtc = match.WrittenAtUtc,
            Kind = MatchSaveDocument.KindMatch,
            Title = match.DisplayTitle,
            Meta = meta,
            CampaignId = campaignId,
            LevelId = match.LevelId,
            ScenarioModuleId = match.ScenarioModuleId,
        };
    }

    public static SaveCatalogEntry FromCampaign(CampaignProgressListEntry campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        return new SaveCatalogEntry
        {
            FilePath = campaign.FilePath,
            WrittenAtUtc = campaign.WrittenAtUtc,
            Kind = CampaignProgressDocument.KindCampaign,
            Title = campaign.DisplayTitle,
            Meta = campaign.DisplayMeta,
            CampaignId = campaign.CampaignId,
            LevelId = campaign.CurrentLevelId,
            ScenarioModuleId = campaign.ScenarioModuleId,
        };
    }
}
