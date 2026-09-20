using System.Text.Json.Serialization;

namespace TinyTBS.Game.Levels.Models;

internal sealed class LevelPlayersDto
{
    [JsonPropertyName("min")]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }

    [JsonPropertyName("defaultSlots")]
    public int DefaultSlots { get; set; }
}
