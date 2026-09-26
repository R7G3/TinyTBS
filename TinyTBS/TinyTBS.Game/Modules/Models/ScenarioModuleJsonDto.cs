using System.Text.Json.Serialization;

namespace TinyTBS.Game.Modules.Models;

internal sealed class ScenarioModuleJsonDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("defaults")]
    public ScenarioDefaultsDto? Defaults { get; set; }

    [JsonPropertyName("requires")]
    public ScenarioRequiresDto? Requires { get; set; }

    [JsonPropertyName("replaces")]
    public List<ScenarioReplaceDto>? Replaces { get; set; }
}

internal sealed class ScenarioDefaultsDto
{
    [JsonPropertyName("units")]
    public List<string>? Units { get; set; }

    [JsonPropertyName("buildings")]
    public List<string>? Buildings { get; set; }

    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
}

internal sealed class ScenarioRequiresDto
{
    [JsonPropertyName("units")]
    public List<string>? Units { get; set; }

    [JsonPropertyName("buildings")]
    public List<string>? Buildings { get; set; }

    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
}

internal sealed class ScenarioReplaceDto
{
    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }
}
