using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Loads a scenario module folder (<c>module.json</c> + Maps/Levels).</summary>
public static class ScenarioModuleLoader
{
    public const string ModuleJsonFileName = "module.json";

    public static ScenarioModuleDefinition Load(string moduleRoot, IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
        ArgumentNullException.ThrowIfNull(files);

        if (!Directory.Exists(moduleRoot))
            throw new MatchContentCompositionException($"Scenario module folder not found: {moduleRoot}");

        var moduleJsonPath = files.Combine(moduleRoot, ModuleJsonFileName);
        if (!files.Exists(moduleJsonPath))
            throw new MatchContentCompositionException($"Missing {ModuleJsonFileName} in {moduleRoot}");

        using var moduleStream = files.OpenRead(moduleJsonPath);
        return ScenarioJsonParser.ParseModuleManifest(moduleStream, moduleRoot);
    }
}
