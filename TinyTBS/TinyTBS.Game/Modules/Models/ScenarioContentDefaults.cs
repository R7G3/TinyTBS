namespace TinyTBS.Game.Modules.Models;

/// <summary>Scenario <c>defaults</c> for match content composition.</summary>
public sealed class ScenarioContentDefaults
{
    public required IReadOnlyList<string> UnitsModuleIds { get; init; }

    public required IReadOnlyList<string> BuildingsModuleIds { get; init; }

    public required string ThemeModuleId { get; init; }
}
