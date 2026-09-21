using TinyTBS.Engine.IO;

namespace TinyTBS.Game.Levels;

/// <summary>
/// Resolves <c>map.ref</c> paths relative to a scenario module root.
/// Only in-module paths under <c>Maps/</c> are allowed (no embed, no escape).
/// </summary>
public static class LevelMapRefResolver
{
    public const string MapsFolderName = "Maps";

    private static readonly char[] LogicalPathSeparators =
    [
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
    ];

    public static string ResolveMapDirectory(
        string scenarioModuleRoot,
        string mapRef,
        IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapRef);
        ArgumentNullException.ThrowIfNull(files);

        var trimmedRef = mapRef.Trim().TrimStart(LogicalPathSeparators);
        if (trimmedRef.Length == 0)
            throw new LevelLoadException("map.ref is empty.");

        if (trimmedRef.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(mapRef))
        {
            throw new LevelLoadException(
                $"map.ref '{mapRef}' must be a relative path inside the scenario module (no '..' or absolute paths).");
        }

        var segments = trimmedRef.Split(LogicalPathSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2
            || !string.Equals(segments[0], MapsFolderName, StringComparison.OrdinalIgnoreCase))
        {
            throw new LevelLoadException(
                $"map.ref '{mapRef}' must start with '{MapsFolderName}/' (e.g. Maps/crossroads).");
        }

        var mapDirectory = files.Combine(scenarioModuleRoot, segments);
        var fullScenarioRoot = Path.GetFullPath(scenarioModuleRoot);
        var fullMapDirectory = Path.GetFullPath(mapDirectory);

        // Ensure trailing separator so "Maps/demo2" is not treated as inside "Maps/demo".
        var scenarioRootPrefix = fullScenarioRoot.TrimEnd(LogicalPathSeparators)
            + Path.DirectorySeparatorChar;
        if (!fullMapDirectory.StartsWith(scenarioRootPrefix, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullMapDirectory, fullScenarioRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new LevelLoadException(
                $"map.ref '{mapRef}' resolves outside the scenario module root.");
        }

        return mapDirectory;
    }
}
