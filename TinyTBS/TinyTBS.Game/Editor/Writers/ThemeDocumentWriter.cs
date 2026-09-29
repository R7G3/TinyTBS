using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Themes;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Themes.Models;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes theme <c>module.json</c> under a theme module folder.</summary>
public sealed class ThemeDocumentWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileContentProvider _files;

    public ThemeDocumentWriter(IFileContentProvider files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string Write(string themeModuleRoot, EditableThemeDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeModuleRoot);
        ArgumentNullException.ThrowIfNull(document);

        ContentModuleManifestParser.ValidateModuleId(document.ModuleId);
        var moduleId = document.ModuleId.Trim();
        var contentNamespace = string.IsNullOrWhiteSpace(document.ContentNamespace)
            ? moduleId
            : document.ContentNamespace.Trim();

        var terrainDirectory = string.IsNullOrWhiteSpace(document.TerrainDirectory)
            ? ThemeModuleDefinition.DefaultTerrainDirectory
            : document.TerrainDirectory.Trim().Replace('\\', '/');
        if (!terrainDirectory.EndsWith('/'))
            terrainDirectory += "/";

        var gravestone = string.IsNullOrWhiteSpace(document.GravestoneRelativePath)
            ? ThemeModuleDefinition.DefaultGravestoneRelativePath
            : document.GravestoneRelativePath.Trim().Replace('\\', '/');

        var remaps = new Dictionary<string, ThemeRemapWriteDto>();
        foreach (var entry in document.Remaps)
        {
            if (string.IsNullOrWhiteSpace(entry.ContentIdFull)
                || string.IsNullOrWhiteSpace(entry.BasePath)
                || string.IsNullOrWhiteSpace(entry.MaskPath))
            {
                continue;
            }

            if (!ContentId.TryParse(entry.ContentIdFull.Trim(), out var contentId))
            {
                throw new EditorException($"Invalid remap content id '{entry.ContentIdFull}'.");
            }

            if (remaps.ContainsKey(contentId.Full))
            {
                throw new EditorException($"Duplicate remap for '{contentId.Full}'.");
            }

            remaps[contentId.Full] = new ThemeRemapWriteDto
            {
                Base = entry.BasePath.Trim().Replace('\\', '/'),
                Mask = entry.MaskPath.Trim().Replace('\\', '/'),
            };
        }

        var payload = new ThemeModuleWriteDto
        {
            FormatVersion = 1,
            Id = moduleId,
            Type = "theme",
            Namespace = contentNamespace,
            Title = string.IsNullOrWhiteSpace(document.Title) ? moduleId : document.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(document.Description) ? null : document.Description.Trim(),
            Version = string.IsNullOrWhiteSpace(document.Version) ? "1.0.0" : document.Version.Trim(),
            Content = new ThemeModuleContentWriteDto
            {
                TerrainDir = terrainDirectory,
                Gravestone = gravestone,
            },
            Remaps = remaps,
        };

        var path = _files.Combine(themeModuleRoot, ContentModuleFiles.ModuleJsonFileName);
        File.WriteAllText(path, JsonSerializer.Serialize(payload, WriteOptions) + Environment.NewLine, Encoding.UTF8);
        document.IsDirty = false;
        return path;
    }

    private sealed class ThemeModuleWriteDto
    {
        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "theme";

        [JsonPropertyName("namespace")]
        public string Namespace { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("content")]
        public ThemeModuleContentWriteDto Content { get; set; } = new();

        [JsonPropertyName("remaps")]
        public Dictionary<string, ThemeRemapWriteDto> Remaps { get; set; } = [];
    }

    private sealed class ThemeModuleContentWriteDto
    {
        [JsonPropertyName("terrainDir")]
        public string TerrainDir { get; set; } = ThemeModuleDefinition.DefaultTerrainDirectory;

        [JsonPropertyName("gravestone")]
        public string Gravestone { get; set; } = ThemeModuleDefinition.DefaultGravestoneRelativePath;
    }

    private sealed class ThemeRemapWriteDto
    {
        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;

        [JsonPropertyName("mask")]
        public string Mask { get; set; } = string.Empty;
    }
}
