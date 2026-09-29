using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Bundles;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes / deletes user <c>{UserData}/Content/Bundles/{id}.bundle.json</c>.</summary>
public sealed class BundleDocumentWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileContentProvider _files;
    private readonly IUserDataPaths _userDataPaths;

    public BundleDocumentWriter(IFileContentProvider files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    public string AllocateUniqueBundleId(string baseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseId);
        ValidateBundleId(baseId.Trim());
        var stem = baseId.Trim();
        _userDataPaths.EnsureCreated();
        if (!File.Exists(UserBundlePath(stem)))
            return stem;

        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = stem + "_" + suffix;
            if (!File.Exists(UserBundlePath(candidate)))
                return candidate;
        }

        throw new EditorException("Could not allocate a unique bundle id.");
    }

    public string Write(EditableBundleDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidateBundleId(document.Id);
        var id = document.Id.Trim();
        ValidateDocument(document);

        _userDataPaths.EnsureCreated();
        Directory.CreateDirectory(_userDataPaths.Bundles);

        var payload = new BundleWriteDto
        {
            FormatVersion = 1,
            Id = id,
            Title = string.IsNullOrWhiteSpace(document.Title) ? id : document.Title.Trim(),
            Modules = document.ModuleIds
                .Where(moduleId => !string.IsNullOrWhiteSpace(moduleId))
                .Select(moduleId => moduleId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            Defaults = new BundleDefaultsWriteDto
            {
                Scenario = document.ScenarioModuleId.Trim(),
                Units = document.UnitsModuleIds
                    .Where(moduleId => !string.IsNullOrWhiteSpace(moduleId))
                    .Select(moduleId => moduleId.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
                Buildings = document.BuildingsModuleIds
                    .Where(moduleId => !string.IsNullOrWhiteSpace(moduleId))
                    .Select(moduleId => moduleId.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
                Theme = document.ThemeModuleId.Trim(),
            },
        };

        var path = UserBundlePath(id);
        File.WriteAllText(path, JsonSerializer.Serialize(payload, WriteOptions) + Environment.NewLine, Encoding.UTF8);

        var originalId = string.IsNullOrWhiteSpace(document.OriginalId) ? id : document.OriginalId.Trim();
        if (!string.Equals(originalId, id, StringComparison.Ordinal))
        {
            var oldPath = UserBundlePath(originalId);
            if (File.Exists(oldPath) && !string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
                File.Delete(oldPath);
        }

        document.OriginalId = id;
        document.IsDirty = false;
        return path;
    }

    public bool Delete(string bundleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleId);
        ValidateBundleId(bundleId.Trim());
        var path = UserBundlePath(bundleId.Trim());
        if (!File.Exists(path))
            return false;

        File.Delete(path);
        return true;
    }

    public string CopyToUserLibrary(ContentBundleDefinition source, string targetBundleId, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ValidateBundleId(targetBundleId);
        var id = targetBundleId.Trim();
        if (File.Exists(UserBundlePath(id)))
            throw new EditorException($"User bundle '{id}' already exists.");

        var document = EditableBundleDocument.FromDefinition(source);
        document.OriginalId = id;
        document.Id = id;
        document.Title = string.IsNullOrWhiteSpace(title) ? source.Title : title.Trim();
        document.IsDirty = true;
        return Write(document);
    }

    private string UserBundlePath(string bundleId) =>
        _files.Combine(_userDataPaths.Bundles, bundleId + ContentBundleFiles.BundleJsonExtension);

    private static void ValidateDocument(EditableBundleDocument document)
    {
        if (document.ModuleIds.Count == 0)
            throw new EditorException("Bundle modules list cannot be empty.");

        var moduleSet = document.ModuleIds
            .Where(moduleId => !string.IsNullOrWhiteSpace(moduleId))
            .Select(moduleId => moduleId.Trim())
            .ToHashSet(StringComparer.Ordinal);

        EnsureListed(moduleSet, document.ScenarioModuleId, "defaults.scenario");
        if (document.UnitsModuleIds.Count == 0)
            throw new EditorException("defaults.units cannot be empty — pick Units in Defaults (from Modules).");
        foreach (var unitsId in document.UnitsModuleIds)
            EnsureListed(moduleSet, unitsId, "defaults.units");

        if (document.BuildingsModuleIds.Count == 0)
            throw new EditorException("defaults.buildings cannot be empty — pick Buildings in Defaults (from Modules).");
        foreach (var buildingsId in document.BuildingsModuleIds)
            EnsureListed(moduleSet, buildingsId, "defaults.buildings");

        if (string.IsNullOrWhiteSpace(document.ThemeModuleId))
        {
            throw new EditorException(
                "defaults.theme is required — enable a theme in Modules (e.g. vanilla_theme) and set Theme in Defaults.");
        }

        EnsureListed(moduleSet, document.ThemeModuleId, "defaults.theme");
    }

    private static void EnsureListed(HashSet<string> moduleSet, string moduleId, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(moduleId) || !moduleSet.Contains(moduleId.Trim()))
        {
            throw new EditorException(
                $"{fieldName} must reference a module listed in modules[] ('{moduleId}').");
        }
    }

    private static void ValidateBundleId(string bundleId)
    {
        if (bundleId.Contains("..", StringComparison.Ordinal)
            || bundleId.Contains('/', StringComparison.Ordinal)
            || bundleId.Contains('\\', StringComparison.Ordinal)
            || bundleId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new EditorException($"Bundle id '{bundleId}' is not a valid file name stem.");
        }
    }

    private sealed class BundleWriteDto
    {
        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("modules")]
        public List<string> Modules { get; set; } = [];

        [JsonPropertyName("defaults")]
        public BundleDefaultsWriteDto Defaults { get; set; } = new();
    }

    private sealed class BundleDefaultsWriteDto
    {
        [JsonPropertyName("scenario")]
        public string Scenario { get; set; } = string.Empty;

        [JsonPropertyName("units")]
        public List<string> Units { get; set; } = [];

        [JsonPropertyName("buildings")]
        public List<string> Buildings { get; set; } = [];

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = string.Empty;
    }
}
