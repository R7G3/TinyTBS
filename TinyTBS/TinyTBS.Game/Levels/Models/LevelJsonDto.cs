using System.Text.Json.Serialization;

namespace TinyTBS.Game.Levels.Models;

/// <summary>Root DTO for level.json deserialization.</summary>
internal sealed class LevelJsonDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("modes")]
    public List<string>? Modes { get; set; }

    [JsonPropertyName("map")]
    public LevelMapRefDto? Map { get; set; }

    [JsonPropertyName("players")]
    public LevelPlayersDto? Players { get; set; }

    [JsonPropertyName("defaultStartingGold")]
    public int? DefaultStartingGold { get; set; }

    [JsonPropertyName("defaultUnitCap")]
    public int? DefaultUnitCap { get; set; }

    [JsonPropertyName("teamDefeatMode")]
    public string? TeamDefeatMode { get; set; }

    [JsonPropertyName("victory")]
    public LevelConditionDto? Victory { get; set; }

    [JsonPropertyName("defeat")]
    public LevelConditionDto? Defeat { get; set; }

    [JsonPropertyName("dialogs")]
    public LevelDialogsDto? Dialogs { get; set; }
}
