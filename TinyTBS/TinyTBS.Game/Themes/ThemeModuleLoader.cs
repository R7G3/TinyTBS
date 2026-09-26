using TinyTBS.Engine.IO;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Themes.Models;

namespace TinyTBS.Game.Themes;

/// <summary>Loads a theme module folder (<c>module.json</c> + <c>Resources/</c>).</summary>
public static class ThemeModuleLoader
{
    public static ThemeModuleDefinition Load(string moduleRoot, IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
        ArgumentNullException.ThrowIfNull(files);

        if (!Directory.Exists(moduleRoot))
            throw new ThemeLoadException($"Theme module folder not found: {moduleRoot}");

        var moduleJsonPath = files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!files.Exists(moduleJsonPath))
            throw new ThemeLoadException($"Missing {ContentModuleFiles.ModuleJsonFileName} in {moduleRoot}");

        string moduleId;
        string contentNamespace;
        string title;
        string version;
        string terrainDirectory;
        string gravestoneRelativePath;
        IReadOnlyDictionary<ContentId, ThemeSpriteRemap> remaps;
        using (var moduleStream = files.OpenRead(moduleJsonPath))
        {
            (moduleId, contentNamespace, title, version, terrainDirectory, gravestoneRelativePath, remaps) =
                ThemeJsonParser.ParseModuleManifest(moduleStream);
        }

        return new ThemeModuleDefinition
        {
            ModuleId = moduleId,
            ContentNamespace = contentNamespace,
            Title = title,
            Version = version,
            ModuleRootPath = moduleRoot,
            TerrainDirectory = terrainDirectory,
            GravestoneRelativePath = gravestoneRelativePath,
            Remaps = remaps,
        };
    }
}
