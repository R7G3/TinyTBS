using System.Text.Json.Serialization;

namespace TinyTBS.Game.Maps.Models;

/// <summary>DTO for one entry in <c>layers.memorials</c>.</summary>
internal sealed class MapMemorialDto
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}
