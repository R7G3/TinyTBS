using System.Text.Json.Serialization;

namespace TinyTBS.Game.Maps.Models;

/// <summary>DTO for one entry in <c>layers.gravestones</c>.</summary>
internal sealed class MapGravestoneDto
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}
