using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Levels.Models;

internal sealed class LevelDialogsDto
{
    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }
}
