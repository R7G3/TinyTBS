using System.Text.Json.Serialization;

namespace TinyTBS.Game.Maps.Models;

/// <summary>DTO for one entry in <c>layers.units</c>.</summary>
internal sealed class MapUnitDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("slot")]
    public int Slot { get; set; }

    [JsonPropertyName("hp")]
    public int? Hp { get; set; }

    [JsonPropertyName("xp")]
    public int? Xp { get; set; }
}
