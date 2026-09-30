using System.Text.Json.Serialization;
using TinyTBS.Rules.Saves.Models;

namespace TinyTBS.Game.Campaigns.Models;

/// <summary>Persistent campaign progress (<c>kind=campaign</c>).</summary>
public sealed class CampaignProgressDocument
{
    public const int CurrentSaveVersion = 1;
    public const string KindCampaign = "campaign";

    [JsonPropertyName("saveVersion")]
    public int SaveVersion { get; init; } = CurrentSaveVersion;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = KindCampaign;

    [JsonPropertyName("writtenAtUtc")]
    public DateTimeOffset WrittenAtUtc { get; init; }

    [JsonPropertyName("campaignId")]
    public string CampaignId { get; init; } = string.Empty;

    [JsonPropertyName("scenarioModuleId")]
    public string ScenarioModuleId { get; init; } = string.Empty;

    [JsonPropertyName("campaignTitle")]
    public string? CampaignTitle { get; init; }

    /// <summary>Chapter the player should play / retry (linear cursor).</summary>
    [JsonPropertyName("currentLevelId")]
    public string CurrentLevelId { get; init; } = string.Empty;

    /// <summary>Highest unlocked chapter level ids (includes current).</summary>
    [JsonPropertyName("unlockedLevelIds")]
    public List<string> UnlockedLevelIds { get; init; } = [];

    /// <summary>Script override for the next chapter after a win (cleared when applied).</summary>
    [JsonPropertyName("pendingNextLevelId")]
    public string? PendingNextLevelId { get; init; }

    [JsonPropertyName("unitCap")]
    public int? UnitCap { get; init; }

    [JsonPropertyName("contentSetup")]
    public MatchSaveContentSetup? ContentSetup { get; init; }

    [JsonPropertyName("playerSeats")]
    public List<MatchSaveSeat> PlayerSeats { get; init; } = [];

    [JsonPropertyName("extensions")]
    public Dictionary<string, string> Extensions { get; init; } = new(StringComparer.Ordinal);
}
