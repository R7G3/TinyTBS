using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Buildings.Models;

internal sealed class BuildingSpritesDto
{
    [JsonPropertyName("base")]
    public string? Base { get; set; }

    [JsonPropertyName("mask")]
    public string? Mask { get; set; }

    [JsonPropertyName("ruinedBase")]
    public string? RuinedBase { get; set; }

    [JsonPropertyName("ruinedMask")]
    public string? RuinedMask { get; set; }
}
