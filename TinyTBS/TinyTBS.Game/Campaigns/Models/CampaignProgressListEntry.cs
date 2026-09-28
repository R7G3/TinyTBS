namespace TinyTBS.Game.Campaigns.Models;

/// <summary>Row for Load Game / Continue discovery.</summary>
public sealed class CampaignProgressListEntry
{
    public required string FilePath { get; init; }

    public required DateTimeOffset WrittenAtUtc { get; init; }

    public required string CampaignId { get; init; }

    public required string ScenarioModuleId { get; init; }

    public required string CurrentLevelId { get; init; }

    public required string Title { get; init; }

    public string DisplayTitle => Title;

    public string DisplayMeta =>
        $"Campaign · {CurrentLevelId} · {WrittenAtUtc.ToUniversalTime():yyyy-MM-dd HH:mm} UTC";
}
