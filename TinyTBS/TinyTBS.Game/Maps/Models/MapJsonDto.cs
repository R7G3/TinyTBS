using System.Text.Json.Serialization;

namespace TinyTBS.Game.Maps.Models;

/// <summary>Root DTO for map.json deserialization.</summary>
internal sealed class MapJsonDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("layers")]
    public MapLayersDto? Layers { get; set; }
}
