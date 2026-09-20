using System.Text.Json.Serialization;

namespace TinyTBS.Game.Levels.Models;

internal sealed class LevelConditionDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
