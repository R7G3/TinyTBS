using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Modules.Models;

/// <summary>Selected scenario + units/buildings/theme module ids for one match.</summary>
public sealed class MatchContentComposition
{
    public required string ScenarioModuleId { get; init; }

    public required IReadOnlyList<string> UnitsModuleIds { get; init; }

    public required IReadOnlyList<string> BuildingsModuleIds { get; init; }

    public required string ThemeModuleId { get; init; }

    public required IReadOnlyList<ContentIdReplace> Replaces { get; init; }

    public static MatchContentComposition FromScenarioDefaults(ScenarioModuleDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        if (scenario.Defaults.UnitsModuleIds.Count == 0)
        {
            throw new MatchContentCompositionException(
                $"Scenario '{scenario.ModuleId}' defaults.units is empty.");
        }

        if (scenario.Defaults.BuildingsModuleIds.Count == 0)
        {
            throw new MatchContentCompositionException(
                $"Scenario '{scenario.ModuleId}' defaults.buildings is empty.");
        }

        if (string.IsNullOrWhiteSpace(scenario.Defaults.ThemeModuleId))
        {
            throw new MatchContentCompositionException(
                $"Scenario '{scenario.ModuleId}' defaults.theme is missing.");
        }

        return new MatchContentComposition
        {
            ScenarioModuleId = scenario.ModuleId,
            UnitsModuleIds = scenario.Defaults.UnitsModuleIds,
            BuildingsModuleIds = scenario.Defaults.BuildingsModuleIds,
            ThemeModuleId = scenario.Defaults.ThemeModuleId,
            Replaces = scenario.Replaces,
        };
    }
}
