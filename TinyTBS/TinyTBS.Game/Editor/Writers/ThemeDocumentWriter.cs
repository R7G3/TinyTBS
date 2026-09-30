using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Themes;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Maps.Models;
using TinyTBS.Rules.Modules.Models;
using TinyTBS.Rules.Themes.Models;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes theme <c>module.json</c> under a theme module folder.</summary>
public sealed class ThemeDocumentWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileSystem _files;

    public ThemeDocumentWriter(IFileSystem files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string Write(string themeModuleRoot, EditableThemeDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeModuleRoot);
        ArgumentNullException.ThrowIfNull(document);

        ContentModuleManifestParser.ValidateModuleId(document.ModuleId);
        var moduleId = document.ModuleId;
        var contentNamespace = string.IsNullOrWhiteSpace(document.ContentNamespace)
            ? moduleId
            : document.ContentNamespace;

        document.Title = SavedUserText.Or(document.Title, moduleId);
        document.Description = SavedUserText.Optional(document.Description);
        document.Version = SavedUserText.Or(document.Version, "1.0.0");
        var terrainDirectory = SavedUserText.Or(
            document.TerrainDirectory,
            ThemeModuleDefinition.DefaultTerrainDirectory).Replace('\\', '/');
        document.TerrainDirectory = terrainDirectory;
        if (!terrainDirectory.EndsWith('/'))
            terrainDirectory += "/";

        var gravestone = SavedUserText.Or(
            document.GravestoneRelativePath,
            ThemeModuleDefinition.DefaultGravestoneRelativePath).Replace('\\', '/');
        document.GravestoneRelativePath = gravestone;

        var remaps = new Dictionary<string, ThemeSpriteRemapDto>();
        foreach (var entry in document.Remaps)
        {
            entry.ContentIdFull = SavedUserText.Trimmed(entry.ContentIdFull);
            entry.BasePath = SavedUserText.Trimmed(entry.BasePath).Replace('\\', '/');
            entry.MaskPath = SavedUserText.Trimmed(entry.MaskPath).Replace('\\', '/');
            if (entry.ContentIdFull.Length == 0
                || entry.BasePath.Length == 0
                || entry.MaskPath.Length == 0)
            {
                continue;
            }

            if (!ContentId.TryParse(entry.ContentIdFull, out var contentId))
                throw new EditorException($"Invalid remap content id '{entry.ContentIdFull}'.");

            if (remaps.ContainsKey(contentId.Full))
                throw new EditorException($"Duplicate remap for '{contentId.Full}'.");

            remaps[contentId.Full] = new ThemeSpriteRemapDto
            {
                Base = entry.BasePath,
                Mask = entry.MaskPath,
            };
        }

        var payload = new ThemeModuleJsonDto
        {
            FormatVersion = 1,
            Id = moduleId,
            Type = ContentModuleTypeIds.Theme,
            Namespace = contentNamespace,
            Title = document.Title,
            Description = document.Description,
            Version = document.Version,
            Content = new ThemeModuleContentDto
            {
                TerrainDir = terrainDirectory,
                Gravestone = gravestone,
            },
            Remaps = remaps,
        };

        var path = _files.Combine(themeModuleRoot, ContentModuleFiles.ModuleJsonFileName);
        _files.WriteAllText(path, JsonSerializer.Serialize(payload, WriteOptions) + Environment.NewLine, Encoding.UTF8);
        document.IsDirty = false;
        return path;
    }
}
