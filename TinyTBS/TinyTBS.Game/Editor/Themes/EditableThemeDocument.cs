using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Rules.Maps.Models;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Themes;
using TinyTBS.Rules.Themes.Models;

namespace TinyTBS.Game.Editor.Themes;

/// <summary>Mutable theme <c>module.json</c> for the Editor master.</summary>
public sealed class EditableThemeDocument
{
    public string ModuleId { get; set; } = "user_theme";

    public string ContentNamespace { get; set; } = "user_theme";

    public string Title { get; set; } = "User Theme";

    public string? Description { get; set; }

    public string Version { get; set; } = "1.0.0";

    public string TerrainDirectory { get; set; } = ThemeModuleDefinition.DefaultTerrainDirectory;

    public string GravestoneRelativePath { get; set; } = ThemeModuleDefinition.DefaultGravestoneRelativePath;

    public List<EditableThemeRemapEntry> Remaps { get; set; } = [];

    public bool IsDirty { get; set; }

    public static EditableThemeDocument Load(string themeModuleRoot, IFileSystem files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeModuleRoot);
        ArgumentNullException.ThrowIfNull(files);

        var definition = ThemeModuleLoader.Load(themeModuleRoot, files);
        var moduleJsonPath = files.Combine(themeModuleRoot, ContentModuleFiles.ModuleJsonFileName);
        string? description = TryReadDescription(files, moduleJsonPath);

        var document = new EditableThemeDocument
        {
            ModuleId = definition.ModuleId,
            ContentNamespace = definition.ContentNamespace,
            Title = definition.Title,
            Description = description,
            Version = definition.Version,
            TerrainDirectory = definition.TerrainDirectory,
            GravestoneRelativePath = definition.GravestoneRelativePath,
            Remaps = definition.Remaps
                .OrderBy(pair => pair.Key.Full, StringComparer.OrdinalIgnoreCase)
                .Select(pair => new EditableThemeRemapEntry
                {
                    ContentIdFull = pair.Key.Full,
                    BasePath = pair.Value.BasePath,
                    MaskPath = pair.Value.MaskPath,
                })
                .ToList(),
            IsDirty = false,
        };
        return document;
    }

    private static string? TryReadDescription(IFileSystem files, string moduleJsonPath)
    {
        if (!files.Exists(moduleJsonPath))
            return null;

        try
        {
            using var jsonDocument = JsonDocument.Parse(files.ReadAllText(moduleJsonPath));
            if (jsonDocument.RootElement.TryGetProperty("description", out var descriptionElement)
                && descriptionElement.ValueKind == JsonValueKind.String)
            {
                var text = descriptionElement.GetString()?.Trim();
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
