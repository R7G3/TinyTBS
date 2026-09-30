using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Saves.Models;

/// <summary>Match content composition + module versions at save time.</summary>
public sealed class MatchSaveContentSetup
{
    [JsonPropertyName("scenarioModuleId")]
    public string ScenarioModuleId { get; init; } = string.Empty;

    [JsonPropertyName("unitsModuleIds")]
    public List<string> UnitsModuleIds { get; init; } = [];

    [JsonPropertyName("buildingsModuleIds")]
    public List<string> BuildingsModuleIds { get; init; } = [];

    [JsonPropertyName("themeModuleId")]
    public string ThemeModuleId { get; init; } = string.Empty;

    [JsonPropertyName("moduleVersions")]
    public Dictionary<string, string> ModuleVersions { get; init; } = new(StringComparer.Ordinal);

    [JsonPropertyName("replaces")]
    public List<MatchSaveReplace> Replaces { get; init; } = [];
}
