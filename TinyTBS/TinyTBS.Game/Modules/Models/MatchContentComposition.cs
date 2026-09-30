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

    /// <summary>
    /// A chosen scenario with units / buildings / theme from a bundle preset; replaces stay the scenario's
    /// because bundles do not carry replaces.
    /// </summary>
    public static MatchContentComposition FromScenarioWithBundle(
        ScenarioModuleDefinition scenario,
        ContentBundleDefinition bundle)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(bundle);

        return new MatchContentComposition
        {
            ScenarioModuleId = scenario.ModuleId,
            UnitsModuleIds = bundle.Defaults.UnitsModuleIds,
            BuildingsModuleIds = bundle.Defaults.BuildingsModuleIds,
            ThemeModuleId = bundle.Defaults.ThemeModuleId,
            Replaces = scenario.Replaces,
        };
    }

    /// <summary>
    /// Builds match composition from a bundle preset.
    /// <paramref name="replaces"/> usually comes from the scenario module (bundles do not carry replaces).
    /// </summary>
    public static MatchContentComposition FromBundleDefaults(
        ContentBundleDefinition bundle,
        IReadOnlyList<ContentIdReplace>? replaces = null)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        if (bundle.Defaults.UnitsModuleIds.Count == 0)
        {
            throw new MatchContentCompositionException(
                $"Bundle '{bundle.BundleId}' defaults.units is empty.");
        }

        if (bundle.Defaults.BuildingsModuleIds.Count == 0)
        {
            throw new MatchContentCompositionException(
                $"Bundle '{bundle.BundleId}' defaults.buildings is empty.");
        }

        if (string.IsNullOrWhiteSpace(bundle.Defaults.ThemeModuleId))
        {
            throw new MatchContentCompositionException(
                $"Bundle '{bundle.BundleId}' defaults.theme is missing.");
        }

        if (string.IsNullOrWhiteSpace(bundle.Defaults.ScenarioModuleId))
        {
            throw new MatchContentCompositionException(
                $"Bundle '{bundle.BundleId}' defaults.scenario is missing.");
        }

        return new MatchContentComposition
        {
            ScenarioModuleId = bundle.Defaults.ScenarioModuleId,
            UnitsModuleIds = bundle.Defaults.UnitsModuleIds,
            BuildingsModuleIds = bundle.Defaults.BuildingsModuleIds,
            ThemeModuleId = bundle.Defaults.ThemeModuleId,
            Replaces = replaces ?? [],
        };
    }
}
