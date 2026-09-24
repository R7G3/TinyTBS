using System.Text.Json.Serialization;

namespace TinyTBS.Game.Buildings.Models;

internal sealed class BuildingModuleJsonDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("content")]
    public BuildingModuleContentDto? Content { get; set; }
}

internal sealed class BuildingModuleContentDto
{
    [JsonPropertyName("buildingsDir")]
    public string? BuildingsDir { get; set; }
}
