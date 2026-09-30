using System.Text.Json;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Rules.Modules;

/// <summary>Parses shared <c>module.json</c> fields for any module type.</summary>
public static class ContentModuleManifestParser
{
    public static ContentModuleInfo Parse(Stream jsonStream, string moduleRootPath, ContentModuleSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRootPath);
        ArgumentNullException.ThrowIfNull(jsonStream);

        ContentModuleManifestDto contentModuleManifest;
        try
        {
            contentModuleManifest = JsonSerializer.Deserialize<ContentModuleManifestDto>(jsonStream, ContentJson.Read)
                ?? throw new TinymodInstallException("module.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new TinymodInstallException("Failed to parse module.json.", jsonException);
        }

        if (contentModuleManifest.FormatVersion < 1)
            throw new TinymodInstallException($"Unsupported formatVersion '{contentModuleManifest.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(contentModuleManifest.Id))
            throw new TinymodInstallException("module.json requires non-empty 'id'.");

        if (string.IsNullOrWhiteSpace(contentModuleManifest.Type))
            throw new TinymodInstallException("module.json requires non-empty 'type'.");

        if (!TryParseType(contentModuleManifest.Type, out var moduleType))
        {
            throw new TinymodInstallException(
                $"Unsupported module type '{contentModuleManifest.Type}'. Expected scenario, units, buildings, or theme.");
        }

        var moduleId = contentModuleManifest.Id.Trim();
        ValidateModuleId(moduleId);

        var contentNamespace = string.IsNullOrWhiteSpace(contentModuleManifest.Namespace)
            ? moduleId
            : contentModuleManifest.Namespace.Trim();

        var title = string.IsNullOrWhiteSpace(contentModuleManifest.Title) ? moduleId : contentModuleManifest.Title.Trim();
        var version = string.IsNullOrWhiteSpace(contentModuleManifest.Version) ? "0.0.0" : contentModuleManifest.Version.Trim();
        var description = string.IsNullOrWhiteSpace(contentModuleManifest.Description)
            ? null
            : contentModuleManifest.Description.Trim();

        return new ContentModuleInfo
        {
            ModuleId = moduleId,
            Type = moduleType,
            ContentNamespace = contentNamespace,
            Title = title,
            Description = description,
            Version = version,
            ModuleRootPath = moduleRootPath,
            Source = source,
        };
    }

    public static bool TryParseType(string? typeValue, out ContentModuleType moduleType) =>
        ContentModuleTypeIds.TryParse(typeValue, out moduleType);

    public static void ValidateModuleId(string moduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);

        if (moduleId.Contains("..", StringComparison.Ordinal)
            || moduleId.Contains('/', StringComparison.Ordinal)
            || moduleId.Contains('\\', StringComparison.Ordinal)
            || moduleId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new TinymodInstallException(
                $"Module id '{moduleId}' is not a valid folder name.");
        }
    }
}
