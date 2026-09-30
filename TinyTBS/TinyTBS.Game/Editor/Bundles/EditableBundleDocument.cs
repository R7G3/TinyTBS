using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Editor.Bundles;

/// <summary>Mutable <c>*.bundle.json</c> for the Editor master.</summary>
public sealed class EditableBundleDocument
{
    public string OriginalId { get; set; } = "user_bundle";

    public string Id { get; set; } = "user_bundle";

    public string Title { get; set; } = "User Bundle";

    public List<string> ModuleIds { get; set; } = [];

    public string ScenarioModuleId { get; set; } = string.Empty;

    public List<string> UnitsModuleIds { get; set; } = [];

    public List<string> BuildingsModuleIds { get; set; } = [];

    public string ThemeModuleId { get; set; } = string.Empty;

    public bool IsDirty { get; set; }

    public static EditableBundleDocument CreateDefault(string bundleId, IReadOnlyList<ContentModuleInfo> availableModules)
    {
        var id = string.IsNullOrWhiteSpace(bundleId) ? "user_bundle" : bundleId.Trim();
        var scenarios = availableModules.Where(module => module.Type == ContentModuleType.Scenario).ToArray();
        var units = availableModules.Where(module => module.Type == ContentModuleType.Units).ToArray();
        var buildings = availableModules.Where(module => module.Type == ContentModuleType.Buildings).ToArray();
        var themes = availableModules.Where(module => module.Type == ContentModuleType.Theme).ToArray();

        var scenarioId = PickPreferred(scenarios, VanillaContentIds.ScenarioModuleId);
        var unitsId = PickPreferred(units, VanillaContentIds.UnitsModuleId);
        var buildingsId = PickPreferred(buildings, VanillaContentIds.BuildingsModuleId);
        var themeId = PickPreferred(themes, VanillaContentIds.ThemeModuleId);

        var moduleIds = new List<string>();
        TryAdd(moduleIds, scenarioId);
        TryAdd(moduleIds, unitsId);
        TryAdd(moduleIds, buildingsId);
        TryAdd(moduleIds, themeId);

        return new EditableBundleDocument
        {
            OriginalId = id,
            Id = id,
            Title = id == "user_bundle" ? "User Bundle" : id,
            ModuleIds = moduleIds,
            ScenarioModuleId = scenarioId,
            UnitsModuleIds = string.IsNullOrWhiteSpace(unitsId) ? [] : [unitsId],
            BuildingsModuleIds = string.IsNullOrWhiteSpace(buildingsId) ? [] : [buildingsId],
            ThemeModuleId = themeId,
            IsDirty = true,
        };
    }

    public static EditableBundleDocument FromDefinition(ContentBundleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new EditableBundleDocument
        {
            OriginalId = definition.BundleId,
            Id = definition.BundleId,
            Title = definition.Title,
            ModuleIds = definition.ModuleIds.ToList(),
            ScenarioModuleId = definition.Defaults.ScenarioModuleId,
            UnitsModuleIds = definition.Defaults.UnitsModuleIds.ToList(),
            BuildingsModuleIds = definition.Defaults.BuildingsModuleIds.ToList(),
            ThemeModuleId = definition.Defaults.ThemeModuleId,
            IsDirty = false,
        };
    }

    public static EditableBundleDocument Load(string bundleFilePath, IFileSystem files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleFilePath);
        ArgumentNullException.ThrowIfNull(files);
        var definition = ContentBundleLoader.Load(bundleFilePath, files, ContentModuleSource.UserLibrary);
        return FromDefinition(definition);
    }

    private static string PickPreferred(IReadOnlyList<ContentModuleInfo> modules, string preferredId)
    {
        if (modules.Count == 0)
            return string.Empty;

        var preferred = modules.FirstOrDefault(
            module => string.Equals(module.ModuleId, preferredId, StringComparison.OrdinalIgnoreCase));
        return preferred?.ModuleId ?? modules[0].ModuleId;
    }

    private static void TryAdd(List<string> moduleIds, string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            return;
        if (moduleIds.Contains(moduleId, StringComparer.Ordinal))
            return;
        moduleIds.Add(moduleId);
    }
}
