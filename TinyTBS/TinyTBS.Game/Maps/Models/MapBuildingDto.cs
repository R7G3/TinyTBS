using System.Text.Json.Serialization;

namespace TinyTBS.Game.Maps.Models;

/// <summary>DTO for one entry in <c>layers.buildings</c>.</summary>
internal sealed class MapBuildingDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("slot")]
    public int? Slot { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }
}
