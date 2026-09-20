using TinyTBS.Engine.IO;
using TinyTBS.Game.Levels.Models;
using TinyTBS.Game.Maps;

namespace TinyTBS.Game.Levels;

/// <summary>Loads a level folder (<c>level.json</c>) and resolves <c>map.ref</c> to a map.</summary>
public static class LevelFolderLoader
{
    public const string LevelJsonFileName = "level.json";

    public static LevelDefinition Load(
        string levelDirectory,
        string scenarioModuleRoot,
        IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentNullException.ThrowIfNull(files);

        if (!Directory.Exists(levelDirectory))
            throw new LevelLoadException($"Level folder not found: {levelDirectory}");

        var levelJsonPath = files.Combine(levelDirectory, LevelJsonFileName);
        if (!files.Exists(levelJsonPath))
            throw new LevelLoadException($"Missing {LevelJsonFileName} in {levelDirectory}");

        string mapRef;
        using (var mapRefStream = files.OpenRead(levelJsonPath))
            mapRef = LevelJsonParser.ReadMapRef(mapRefStream);

        var mapDirectory = LevelMapRefResolver.ResolveMapDirectory(scenarioModuleRoot, mapRef, files);

        try
        {
            var map = MapFolderLoader.Load(mapDirectory, files);

            using var levelJsonStream = files.OpenRead(levelJsonPath);

            return LevelJsonParser.Parse(
                levelJsonStream,
                map,
                mapRef,
                sourceDirectory: levelDirectory,
                scenarioModuleRoot: scenarioModuleRoot);
        }
        catch (MapLoadException mapLoadException)
        {
            throw new LevelLoadException(
                $"Failed to load map for level at '{levelDirectory}' (map.ref '{mapRef}').",
                mapLoadException);
        }
    }

    /// <summary>
    /// Loads <c>Levels/{levelId}/</c> under a scenario module root
    /// (or any root that contains <c>Levels</c> and <c>Maps</c>).
    /// </summary>
    public static LevelDefinition LoadFromModuleLevels(
        string scenarioModuleRoot,
        string levelId,
        IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelId);
        var levelDirectory = files.Combine(scenarioModuleRoot, "Levels", levelId);
        return Load(levelDirectory, scenarioModuleRoot, files);
    }
}
