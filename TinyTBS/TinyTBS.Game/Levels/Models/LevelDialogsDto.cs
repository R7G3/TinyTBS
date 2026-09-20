using System.Text.Json.Serialization;

namespace TinyTBS.Game.Levels.Models;

internal sealed class LevelDialogsDto
{
    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }
}
