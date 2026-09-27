using System.Text.Json;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Parses <c>*.bundle.json</c> presets.</summary>
public static class ContentBundleJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ContentBundleDefinition Parse(
        Stream jsonStream,
        string bundleFilePath,
        ContentModuleSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleFilePath);
        ArgumentNullException.ThrowIfNull(jsonStream);

        ContentBundleJsonDto document;
        try
        {
            document = JsonSerializer.Deserialize<ContentBundleJsonDto>(jsonStream, JsonOptions)
                ?? throw new ContentBundleException("bundle.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new ContentBundleException("Failed to parse bundle.json.", jsonException);
        }

        if (document.FormatVersion < 1)
            throw new ContentBundleException($"Unsupported formatVersion '{document.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(document.Id))
            throw new ContentBundleException("bundle.json requires non-empty 'id'.");

        var bundleId = document.Id.Trim();
        ValidateBundleId(bundleId);

        var moduleIds = NormalizeIdList(document.Modules);
        if (moduleIds.Count == 0)
            throw new ContentBundleException($"Bundle '{bundleId}' modules list is empty.");

        var moduleIdSet = moduleIds.ToHashSet(StringComparer.Ordinal);
        var defaults = ParseDefaults(document.Defaults, bundleId, moduleIdSet);
        var title = string.IsNullOrWhiteSpace(document.Title) ? bundleId : document.Title.Trim();

        return new ContentBundleDefinition
        {
            BundleId = bundleId,
            Title = title,
            ModuleIds = moduleIds,
            Defaults = defaults,
            BundleFilePath = bundleFilePath,
            Source = source,
        };
    }

    private static ContentBundleDefaults ParseDefaults(
        ContentBundleDefaultsDto? defaults,
        string bundleId,
        HashSet<string> moduleIdSet)
    {
        if (defaults is null)
            throw new ContentBundleException($"Bundle '{bundleId}' requires 'defaults'.");

        if (string.IsNullOrWhiteSpace(defaults.Scenario))
            throw new ContentBundleException($"Bundle '{bundleId}' defaults.scenario is missing.");

        var scenarioModuleId = defaults.Scenario.Trim();
        EnsureListed(moduleIdSet, scenarioModuleId, bundleId, "defaults.scenario");

        var units = NormalizeIdList(defaults.Units);
        if (units.Count == 0)
            throw new ContentBundleException($"Bundle '{bundleId}' defaults.units is empty.");

        foreach (var unitsModuleId in units)
            EnsureListed(moduleIdSet, unitsModuleId, bundleId, "defaults.units");

        var buildings = NormalizeIdList(defaults.Buildings);
        if (buildings.Count == 0)
            throw new ContentBundleException($"Bundle '{bundleId}' defaults.buildings is empty.");

        foreach (var buildingsModuleId in buildings)
            EnsureListed(moduleIdSet, buildingsModuleId, bundleId, "defaults.buildings");

        if (string.IsNullOrWhiteSpace(defaults.Theme))
            throw new ContentBundleException($"Bundle '{bundleId}' defaults.theme is missing.");

        var themeModuleId = defaults.Theme.Trim();
        EnsureListed(moduleIdSet, themeModuleId, bundleId, "defaults.theme");

        return new ContentBundleDefaults
        {
            ScenarioModuleId = scenarioModuleId,
            UnitsModuleIds = units,
            BuildingsModuleIds = buildings,
            ThemeModuleId = themeModuleId,
        };
    }

    private static void EnsureListed(
        HashSet<string> moduleIdSet,
        string moduleId,
        string bundleId,
        string fieldName)
    {
        if (!moduleIdSet.Contains(moduleId))
        {
            throw new ContentBundleException(
                $"Bundle '{bundleId}' {fieldName} references '{moduleId}', which is not in modules[].");
        }
    }

    private static void ValidateBundleId(string bundleId)
    {
        if (bundleId.Contains("..", StringComparison.Ordinal)
            || bundleId.Contains('/', StringComparison.Ordinal)
            || bundleId.Contains('\\', StringComparison.Ordinal)
            || bundleId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ContentBundleException($"Bundle id '{bundleId}' is not a valid file name stem.");
        }
    }

    private static IReadOnlyList<string> NormalizeIdList(List<string>? values)
    {
        if (values is null || values.Count == 0)
            return [];

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;
            var trimmed = value.Trim();
            if (!seen.Add(trimmed))
                continue;
            result.Add(trimmed);
        }

        return result;
    }
}
