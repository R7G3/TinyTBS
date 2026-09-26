using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Themes.Models;

/// <summary>Loaded theme module: remaps + conventional terrain/gravestone art paths.</summary>
public sealed class ThemeModuleDefinition
{
    public const string DefaultTerrainDirectory = "Resources/Images/terrain/";

    public const string DefaultGravestoneRelativePath = "Resources/Images/misc/gravestone.png";

    public required string ModuleId { get; init; }

    public required string ContentNamespace { get; init; }

    public required string Title { get; init; }

    public required string Version { get; init; }

    public required string ModuleRootPath { get; init; }

    /// <summary>Module-relative directory for terrain tiles (trailing slash optional).</summary>
    public required string TerrainDirectory { get; init; }

    /// <summary>Module-relative path to gravestone art.</summary>
    public required string GravestoneRelativePath { get; init; }

    /// <summary>Sprite remaps keyed by logical content id (<c>namespace/localId</c>).</summary>
    public required IReadOnlyDictionary<ContentId, ThemeSpriteRemap> Remaps { get; init; }

    /// <summary>Module-relative path for a terrain tile file name (e.g. <c>grass.png</c>).</summary>
    public string TerrainRelativePath(string terrainFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(terrainFileName);
        var directory = TerrainDirectory.Replace('\\', '/').TrimEnd('/');
        return $"{directory}/{terrainFileName.TrimStart('/')}";
    }
}
