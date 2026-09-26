namespace TinyTBS.Game.Modules.Models;

/// <summary>Scenario <c>requires</c> — modules that must be present in the match content composition.</summary>
public sealed class ScenarioContentRequires
{
    public IReadOnlyList<string> UnitsModuleIds { get; init; } = [];

    public IReadOnlyList<string> BuildingsModuleIds { get; init; } = [];

    public string? ThemeModuleId { get; init; }
}
