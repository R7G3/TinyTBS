namespace TinyTBS.Game.Campaigns.Models;

/// <summary>One chapter entry in <c>campaign.json</c>.</summary>
public sealed class CampaignChapterDefinition
{
    public required string LevelId { get; init; }

    /// <summary>Path from scenario module root (e.g. <c>Levels/campaign-01</c>).</summary>
    public required string Path { get; init; }
}
