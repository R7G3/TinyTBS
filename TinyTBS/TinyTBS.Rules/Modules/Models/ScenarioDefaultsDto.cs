using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Modules.Models;

internal sealed class ScenarioDefaultsDto
{
    [JsonPropertyName("units")]
    public List<string>? Units { get; set; }

    [JsonPropertyName("buildings")]
    public List<string>? Buildings { get; set; }

    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
}
