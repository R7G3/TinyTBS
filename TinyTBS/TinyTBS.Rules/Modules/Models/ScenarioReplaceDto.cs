using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Modules.Models;

internal sealed class ScenarioReplaceDto
{
    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }
}
