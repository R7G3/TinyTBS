using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Levels.Models;

internal sealed class LevelConditionDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
