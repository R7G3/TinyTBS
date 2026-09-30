using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Modules.Models;

internal sealed class ScenarioContentDto
{
    [JsonPropertyName("campaign")]
    public string? Campaign { get; set; }
}
