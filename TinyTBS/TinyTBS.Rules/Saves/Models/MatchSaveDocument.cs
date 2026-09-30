using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Saves.Models;

/// <summary>Match save document (kind = match).</summary>
public sealed class MatchSaveDocument
{
    public const int CurrentSaveVersion = 1;

    public const string KindMatch = "match";

    [JsonPropertyName("saveVersion")]
    public int SaveVersion { get; init; }

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = KindMatch;

    [JsonPropertyName("writtenAtUtc")]
    public DateTimeOffset WrittenAtUtc { get; init; }

    [JsonPropertyName("levelId")]
    public string LevelId { get; init; } = string.Empty;

    /// <summary>When set, this match belongs to a campaign run.</summary>
    [JsonPropertyName("campaignId")]
    public string? CampaignId { get; init; }

    /// <summary>Chapter level id inside the campaign (usually same as <see cref="LevelId"/>).</summary>
    [JsonPropertyName("campaignLevelId")]
    public string? CampaignLevelId { get; init; }

    [JsonPropertyName("unitCap")]
    public int UnitCap { get; init; }

    [JsonPropertyName("contentSetup")]
    public MatchSaveContentSetup ContentSetup { get; init; } = null!;

    [JsonPropertyName("playerSeats")]
    public List<MatchSaveSeat> PlayerSeats { get; init; } = [];

    [JsonPropertyName("match")]
    public MatchRuntimeSnapshot Match { get; init; } = null!;

    /// <summary>Reserved for map/campaign script flags.</summary>
    [JsonPropertyName("extensions")]
    public Dictionary<string, string> Extensions { get; init; } = new(StringComparer.Ordinal);
}
