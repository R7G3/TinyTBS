using TinyTBS.Engine.IO;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Loads a scenario module folder (<c>module.json</c> + Maps/Levels).</summary>
public static class ScenarioModuleLoader
{
    public static ScenarioModuleDefinition Load(string moduleRoot, IFileSystem files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
        ArgumentNullException.ThrowIfNull(files);

        if (!files.DirectoryExists(moduleRoot))
            throw new MatchContentCompositionException($"Scenario module folder not found: {moduleRoot}");

        var moduleJsonPath = files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!files.Exists(moduleJsonPath))
            throw new MatchContentCompositionException(
                $"Missing {ContentModuleFiles.ModuleJsonFileName} in {moduleRoot}");

        using var moduleStream = files.OpenRead(moduleJsonPath);
        return ScenarioJsonParser.ParseModuleManifest(moduleStream, moduleRoot);
    }
}
