using System.Text.Json;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Parses scenario <c>module.json</c>.</summary>
public static class ScenarioJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ScenarioModuleDefinition ParseModuleManifest(Stream jsonStream, string moduleRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRootPath);

        ScenarioModuleJsonDto document;
        try
        {
            document = JsonSerializer.Deserialize<ScenarioModuleJsonDto>(jsonStream, JsonOptions)
                ?? throw new MatchContentCompositionException("module.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new MatchContentCompositionException("Failed to parse scenario module.json.", jsonException);
        }

        if (document.FormatVersion < 1)
            throw new MatchContentCompositionException($"Unsupported formatVersion '{document.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(document.Id))
            throw new MatchContentCompositionException("module.json requires non-empty 'id'.");

        if (!string.Equals(document.Type, "scenario", StringComparison.OrdinalIgnoreCase))
            throw new MatchContentCompositionException($"Expected module type 'scenario', got '{document.Type}'.");

        var contentNamespace = string.IsNullOrWhiteSpace(document.Namespace)
            ? document.Id.Trim()
            : document.Namespace.Trim();

        var defaults = ParseDefaults(document.Defaults, document.Id.Trim());
        var requires = ParseRequires(document.Requires);
        var replaces = ParseReplaces(document.Replaces);

        var title = string.IsNullOrWhiteSpace(document.Title) ? document.Id.Trim() : document.Title.Trim();
        var version = string.IsNullOrWhiteSpace(document.Version) ? "0.0.0" : document.Version.Trim();

        return new ScenarioModuleDefinition
        {
            ModuleId = document.Id.Trim(),
            ContentNamespace = contentNamespace,
            Title = title,
            Version = version,
            ModuleRootPath = moduleRootPath,
            Defaults = defaults,
            Requires = requires,
            Replaces = replaces,
        };
    }

    private static ScenarioContentDefaults ParseDefaults(ScenarioDefaultsDto? defaults, string scenarioId)
    {
        if (defaults is null)
            throw new MatchContentCompositionException($"Scenario '{scenarioId}' requires 'defaults'.");

        var units = NormalizeIdList(defaults.Units);
        var buildings = NormalizeIdList(defaults.Buildings);
        if (units.Count == 0)
            throw new MatchContentCompositionException($"Scenario '{scenarioId}' defaults.units is empty.");
        if (buildings.Count == 0)
            throw new MatchContentCompositionException($"Scenario '{scenarioId}' defaults.buildings is empty.");
        if (string.IsNullOrWhiteSpace(defaults.Theme))
            throw new MatchContentCompositionException($"Scenario '{scenarioId}' defaults.theme is missing.");

        return new ScenarioContentDefaults
        {
            UnitsModuleIds = units,
            BuildingsModuleIds = buildings,
            ThemeModuleId = defaults.Theme.Trim(),
        };
    }

    private static ScenarioContentRequires ParseRequires(ScenarioRequiresDto? requires)
    {
        if (requires is null)
            return new ScenarioContentRequires();

        return new ScenarioContentRequires
        {
            UnitsModuleIds = NormalizeIdList(requires.Units),
            BuildingsModuleIds = NormalizeIdList(requires.Buildings),
            ThemeModuleId = string.IsNullOrWhiteSpace(requires.Theme) ? null : requires.Theme.Trim(),
        };
    }

    private static IReadOnlyList<ContentIdReplace> ParseReplaces(List<ScenarioReplaceDto>? replaces)
    {
        if (replaces is null || replaces.Count == 0)
            return [];

        var result = new List<ContentIdReplace>(replaces.Count);
        var seenFrom = new HashSet<ContentId>();
        for (var i = 0; i < replaces.Count; i++)
        {
            var entry = replaces[i];
            if (entry is null
                || !ContentId.TryParse(entry.From, out var fromId)
                || !ContentId.TryParse(entry.To, out var toId))
            {
                throw new MatchContentCompositionException(
                    $"replaces[{i}] requires valid 'from' and 'to' content ids.");
            }

            if (!seenFrom.Add(fromId))
                throw new MatchContentCompositionException($"Duplicate replaces.from '{fromId.Full}'.");

            result.Add(new ContentIdReplace { From = fromId, To = toId });
        }

        return result;
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
