using System.Text.Json;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Themes.Models;

namespace TinyTBS.Game.Themes;

/// <summary>Parses theme <c>module.json</c>.</summary>
public static class ThemeJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static (
        string ModuleId,
        string ContentNamespace,
        string Title,
        string Version,
        string TerrainDirectory,
        string GravestoneRelativePath,
        IReadOnlyDictionary<ContentId, ThemeSpriteRemap> Remaps)
        ParseModuleManifest(Stream jsonStream)
    {
        ThemeModuleJsonDto document;
        try
        {
            document = JsonSerializer.Deserialize<ThemeModuleJsonDto>(jsonStream, JsonOptions)
                ?? throw new ThemeLoadException("module.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new ThemeLoadException("Failed to parse theme module.json.", jsonException);
        }

        if (document.FormatVersion < 1)
            throw new ThemeLoadException($"Unsupported formatVersion '{document.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(document.Id))
            throw new ThemeLoadException("module.json requires non-empty 'id'.");

        if (!string.Equals(document.Type, "theme", StringComparison.OrdinalIgnoreCase))
            throw new ThemeLoadException($"Expected module type 'theme', got '{document.Type}'.");

        var contentNamespace = string.IsNullOrWhiteSpace(document.Namespace)
            ? document.Id.Trim()
            : document.Namespace.Trim();

        var terrainDirectory = document.Content?.TerrainDir;
        if (string.IsNullOrWhiteSpace(terrainDirectory))
            terrainDirectory = ThemeModuleDefinition.DefaultTerrainDirectory;

        var gravestoneRelativePath = document.Content?.Gravestone;
        if (string.IsNullOrWhiteSpace(gravestoneRelativePath))
            gravestoneRelativePath = ThemeModuleDefinition.DefaultGravestoneRelativePath;

        var remaps = new Dictionary<ContentId, ThemeSpriteRemap>();
        if (document.Remaps is { Count: > 0 })
        {
            foreach (var (rawContentId, remapDto) in document.Remaps)
            {
                if (!ContentId.TryParse(rawContentId, out var contentId))
                {
                    throw new ThemeLoadException(
                        $"Invalid remap content id '{rawContentId}'.");
                }

                if (remapDto is null
                    || string.IsNullOrWhiteSpace(remapDto.Base)
                    || string.IsNullOrWhiteSpace(remapDto.Mask))
                {
                    throw new ThemeLoadException(
                        $"Remap '{rawContentId}' requires base and mask paths.");
                }

                if (!remaps.TryAdd(
                        contentId,
                        new ThemeSpriteRemap
                        {
                            BasePath = remapDto.Base.Trim(),
                            MaskPath = remapDto.Mask.Trim(),
                        }))
                {
                    throw new ThemeLoadException($"Duplicate remap for '{contentId.Full}'.");
                }
            }
        }

        var title = string.IsNullOrWhiteSpace(document.Title) ? document.Id.Trim() : document.Title.Trim();
        var version = string.IsNullOrWhiteSpace(document.Version) ? "0.0.0" : document.Version.Trim();

        return (
            document.Id.Trim(),
            contentNamespace,
            title,
            version,
            terrainDirectory.Trim(),
            gravestoneRelativePath.Trim(),
            remaps);
    }
}
