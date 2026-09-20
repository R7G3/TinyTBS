using TinyTBS.Engine.IO;
using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Maps;

/// <summary>Loads a map folder (<c>map.json</c> + optional <c>script.cs</c>).</summary>
public static class MapFolderLoader
{
    public const string MapJsonFileName = "map.json";
    public const string ScriptFileName = "script.cs";

    public static MapDefinition Load(string mapDirectory, IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapDirectory);
        ArgumentNullException.ThrowIfNull(files);

        if (!Directory.Exists(mapDirectory))
            throw new MapLoadException($"Map folder not found: {mapDirectory}");

        var mapJsonPath = files.Combine(mapDirectory, MapJsonFileName);
        if (!files.Exists(mapJsonPath))
            throw new MapLoadException($"Missing {MapJsonFileName} in {mapDirectory}");

        var scriptFilePath = files.Combine(mapDirectory, ScriptFileName);
        string? scriptPath = files.Exists(scriptFilePath) ? scriptFilePath : null;

        using var mapJsonStream = files.OpenRead(mapJsonPath);
        return MapJsonParser.Parse(mapJsonStream, mapDirectory, scriptPath);
    }

    /// <summary>
    /// Loads <c>Maps/{mapId}/</c> under a scenario module root
    /// (or any root that contains a <c>Maps</c> folder).
    /// </summary>
    public static MapDefinition LoadFromModuleMaps(
        string moduleOrScenarioRoot,
        string mapId,
        IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);
        var mapDirectory = files.Combine(moduleOrScenarioRoot, "Maps", mapId);
        return Load(mapDirectory, files);
    }
}
