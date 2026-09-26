using TinyTBS.Engine.IO;
using TinyTBS.Game.Buildings;
using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Match;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Themes;
using TinyTBS.Game.Themes.Models;
using TinyTBS.Game.Units;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Loads and validates a <see cref="MatchContentComposition"/> into a match content catalog.</summary>
public static class MatchContentCompositionLoader
{
    public static MatchContentLoadResult Load(
        MatchContentComposition composition,
        ContentModuleLocator locator,
        IFileContentProvider files)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(files);

        var scenarioRoot = locator.ResolveModuleRoot(composition.ScenarioModuleId);
        var scenario = ScenarioModuleLoader.Load(scenarioRoot, files);
        EnsureRequiresSatisfied(composition, scenario.Requires);

        var errors = new List<string>();
        var unitsModules = new List<UnitModuleDefinition>();
        foreach (var moduleId in composition.UnitsModuleIds)
        {
            try
            {
                unitsModules.Add(UnitModuleLoader.Load(locator.ResolveModuleRoot(moduleId), files));
            }
            catch (Exception exception) when (exception is not MatchContentCompositionException)
            {
                errors.Add($"Units module '{moduleId}': {exception.Message}");
            }
        }

        var buildingsModules = new List<BuildingModuleDefinition>();
        foreach (var moduleId in composition.BuildingsModuleIds)
        {
            try
            {
                buildingsModules.Add(BuildingModuleLoader.Load(locator.ResolveModuleRoot(moduleId), files));
            }
            catch (Exception exception) when (exception is not MatchContentCompositionException)
            {
                errors.Add($"Buildings module '{moduleId}': {exception.Message}");
            }
        }

        ThemeModuleDefinition? themeModule = null;
        try
        {
            themeModule = ThemeModuleLoader.Load(locator.ResolveModuleRoot(composition.ThemeModuleId), files);
        }
        catch (Exception exception) when (exception is not MatchContentCompositionException)
        {
            errors.Add($"Theme module '{composition.ThemeModuleId}': {exception.Message}");
        }

        if (errors.Count > 0)
            throw new MatchContentCompositionException(errors);

        var mergedUnits = MergeUnits(unitsModules, errors);
        var mergedBuildings = MergeBuildings(buildingsModules, errors);
        if (errors.Count > 0)
            throw new MatchContentCompositionException(errors);

        ArgumentNullException.ThrowIfNull(themeModule);

        var catalog = new MatchContentCatalog(mergedUnits, mergedBuildings, themeModule);
        var replaces = new ContentIdReplaceTable(composition.Replaces);
        ValidateReplaceTargets(composition.Replaces, catalog, errors);
        if (errors.Count > 0)
            throw new MatchContentCompositionException(errors);

        return new MatchContentLoadResult
        {
            Composition = composition,
            Scenario = scenario,
            Catalog = catalog,
            Replaces = replaces,
        };
    }

    /// <summary>Ensures every unit/building type on the map resolves in the catalog after replaces.</summary>
    public static void ValidateMapTypes(
        MapDefinition map,
        MatchContentCatalog catalog,
        ContentIdReplaceTable replaces)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(replaces);

        var errors = new List<string>();
        for (var i = 0; i < map.Buildings.Count; i++)
        {
            var mapType = map.Buildings[i].Type;
            var resolved = replaces.Resolve(mapType);
            if (!catalog.TryGetBuilding(resolved, out _))
            {
                errors.Add(
                    $"buildings[{i}] type '{mapType.Full}'"
                    + (resolved.Equals(mapType) ? "" : $" → '{resolved.Full}'")
                    + " is not in the match content composition.");
            }
        }

        for (var i = 0; i < map.Units.Count; i++)
        {
            var mapType = map.Units[i].Type;
            var resolved = replaces.Resolve(mapType);
            if (!catalog.TryGetUnit(resolved, out _))
            {
                errors.Add(
                    $"units[{i}] type '{mapType.Full}'"
                    + (resolved.Equals(mapType) ? "" : $" → '{resolved.Full}'")
                    + " is not in the match content composition.");
            }
        }

        if (errors.Count > 0)
            throw new MatchContentCompositionException(errors);
    }

    private static void EnsureRequiresSatisfied(
        MatchContentComposition composition,
        ScenarioContentRequires requires)
    {
        var errors = new List<string>();
        var unitsSet = composition.UnitsModuleIds.ToHashSet(StringComparer.Ordinal);
        var buildingsSet = composition.BuildingsModuleIds.ToHashSet(StringComparer.Ordinal);

        foreach (var requiredId in requires.UnitsModuleIds)
        {
            if (!unitsSet.Contains(requiredId))
                errors.Add($"Missing required units module '{requiredId}'.");
        }

        foreach (var requiredId in requires.BuildingsModuleIds)
        {
            if (!buildingsSet.Contains(requiredId))
                errors.Add($"Missing required buildings module '{requiredId}'.");
        }

        if (!string.IsNullOrWhiteSpace(requires.ThemeModuleId)
            && !string.Equals(composition.ThemeModuleId, requires.ThemeModuleId, StringComparison.Ordinal))
        {
            errors.Add($"Theme must be '{requires.ThemeModuleId}' (required by scenario).");
        }

        if (errors.Count > 0)
            throw new MatchContentCompositionException(errors);
    }

    private static void ValidateReplaceTargets(
        IReadOnlyList<ContentIdReplace> replaces,
        MatchContentCatalog catalog,
        List<string> errors)
    {
        foreach (var entry in replaces)
        {
            if (catalog.TryGetUnit(entry.To, out _) || catalog.TryGetBuilding(entry.To, out _))
                continue;

            errors.Add(
                $"Replace target '{entry.To.Full}' (from '{entry.From.Full}') is not in the match content composition.");
        }
    }

    private static UnitModuleDefinition MergeUnits(
        IReadOnlyList<UnitModuleDefinition> modules,
        List<string> errors)
    {
        var unitsById = new Dictionary<ContentId, UnitDefinition>();
        var recruitPool = new List<ContentId>();
        var ownerById = new Dictionary<ContentId, string>();

        foreach (var module in modules)
        {
            foreach (var (contentId, definition) in module.UnitsById)
            {
                if (!unitsById.TryAdd(contentId, definition))
                {
                    errors.Add(
                        $"Duplicate unit id '{contentId.Full}' in modules '{ownerById[contentId]}' and '{module.ModuleId}'.");
                    continue;
                }

                ownerById[contentId] = module.ModuleId;
            }

            foreach (var recruitId in module.RecruitPool)
            {
                if (!recruitPool.Contains(recruitId))
                    recruitPool.Add(recruitId);
            }
        }

        if (unitsById.Count == 0)
            errors.Add("Match content composition has no unit definitions.");

        return new UnitModuleDefinition
        {
            ModuleId = "match_content_units",
            ContentNamespace = "match_content",
            Title = "Match Content Units",
            Version = "0.0.0",
            RecruitPool = recruitPool,
            UnitsById = unitsById,
            ModuleRootPath = modules.Count == 1 ? modules[0].ModuleRootPath : string.Empty,
        };
    }

    private static BuildingModuleDefinition MergeBuildings(
        IReadOnlyList<BuildingModuleDefinition> modules,
        List<string> errors)
    {
        var buildingsById = new Dictionary<ContentId, BuildingDefinition>();
        var ownerById = new Dictionary<ContentId, string>();

        foreach (var module in modules)
        {
            foreach (var (contentId, definition) in module.BuildingsById)
            {
                if (!buildingsById.TryAdd(contentId, definition))
                {
                    errors.Add(
                        $"Duplicate building id '{contentId.Full}' in modules '{ownerById[contentId]}' and '{module.ModuleId}'.");
                    continue;
                }

                ownerById[contentId] = module.ModuleId;
            }
        }

        if (buildingsById.Count == 0)
            errors.Add("Match content composition has no building definitions.");

        return new BuildingModuleDefinition
        {
            ModuleId = "match_content_buildings",
            ContentNamespace = "match_content",
            Title = "Match Content Buildings",
            Version = "0.0.0",
            BuildingsById = buildingsById,
            ModuleRootPath = modules.Count == 1 ? modules[0].ModuleRootPath : string.Empty,
        };
    }
}
