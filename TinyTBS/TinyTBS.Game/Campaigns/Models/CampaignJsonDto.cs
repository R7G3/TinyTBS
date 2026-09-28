using System.Text.Json.Serialization;

namespace TinyTBS.Game.Campaigns.Models;

internal sealed class CampaignJsonDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("levels")]
    public List<CampaignLevelJsonDto>? Levels { get; set; }
}

internal sealed class CampaignLevelJsonDto
{
    [JsonPropertyName("levelId")]
    public string? LevelId { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}
