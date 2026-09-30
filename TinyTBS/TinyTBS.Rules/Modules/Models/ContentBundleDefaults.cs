namespace TinyTBS.Rules.Modules.Models;

/// <summary>Preset defaults from a <c>*.bundle.json</c> (scenario + match content module ids).</summary>
public sealed class ContentBundleDefaults
{
    public required string ScenarioModuleId { get; init; }

    public required IReadOnlyList<string> UnitsModuleIds { get; init; }

    public required IReadOnlyList<string> BuildingsModuleIds { get; init; }

    public required string ThemeModuleId { get; init; }
}
