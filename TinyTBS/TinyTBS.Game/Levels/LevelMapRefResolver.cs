using TinyTBS.Engine.IO;

namespace TinyTBS.Game.Levels;

/// <summary>
/// Resolves <c>map.ref</c> paths relative to a scenario module root.
/// Only in-module paths under <c>Maps/</c> are allowed (no embed, no escape).
/// </summary>
public static class LevelMapRefResolver
{
    public const string MapsFolderName = "Maps";

    public static string ResolveMapDirectory(
        string scenarioModuleRoot,
        string mapRef,
        IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapRef);
        ArgumentNullException.ThrowIfNull(files);

        var normalizedRef = mapRef
            .Replace('\\', '/')
            .Trim()
            .TrimStart('/');

        if (normalizedRef.Length == 0)
            throw new LevelLoadException("map.ref is empty.");

        if (normalizedRef.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(mapRef))
        {
            throw new LevelLoadException(
                $"map.ref '{mapRef}' must be a relative path inside the scenario module (no '..' or absolute paths).");
        }

        var segments = normalizedRef.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2
            || !string.Equals(segments[0], MapsFolderName, StringComparison.OrdinalIgnoreCase))
        {
            throw new LevelLoadException(
                $"map.ref '{mapRef}' must start with '{MapsFolderName}/' (e.g. Maps/crossroads).");
        }

        var mapDirectory = files.Combine(scenarioModuleRoot, segments);
        var fullScenarioRoot = Path.GetFullPath(scenarioModuleRoot);
        var fullMapDirectory = Path.GetFullPath(mapDirectory);

        if (!fullMapDirectory.StartsWith(fullScenarioRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new LevelLoadException(
                $"map.ref '{mapRef}' resolves outside the scenario module root.");
        }

        return mapDirectory;
    }
}
