using System.Text;
using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Creates empty user units or buildings modules.</summary>
public sealed class ContentTypeModuleWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileContentProvider _files;
    private readonly IUserDataPaths _userDataPaths;

    public ContentTypeModuleWriter(IFileContentProvider files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    public string AllocateUniqueModuleId(string baseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseId);
        ContentModuleManifestParser.ValidateModuleId(baseId.Trim());
        var stem = baseId.Trim();
        _userDataPaths.EnsureCreated();
        if (!Directory.Exists(_files.Combine(_userDataPaths.Modules, stem)))
            return stem;

        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = stem + "_" + suffix;
            if (!Directory.Exists(_files.Combine(_userDataPaths.Modules, candidate)))
                return candidate;
        }

        throw new EditorException("Could not allocate a unique module id.");
    }

    public string CreateNew(
        ContentModuleType type,
        string moduleId,
        string title,
        string? description = null)
    {
        if (type is not (ContentModuleType.Units or ContentModuleType.Buildings))
            throw new EditorException("Only units or buildings modules can be created here.");

        ContentModuleManifestParser.ValidateModuleId(moduleId.Trim());
        var id = moduleId.Trim();
        _userDataPaths.EnsureCreated();
        var moduleRoot = _files.Combine(_userDataPaths.Modules, id);
        if (Directory.Exists(moduleRoot))
            throw new EditorException($"User module '{id}' already exists.");

        var typeName = type == ContentModuleType.Units ? "units" : "buildings";
        var contentFolder = type == ContentModuleType.Units ? "Units" : "Buildings";

        try
        {
            Directory.CreateDirectory(moduleRoot);
            Directory.CreateDirectory(_files.Combine(moduleRoot, contentFolder));
            Directory.CreateDirectory(_files.Combine(moduleRoot, "Resources", "Images", contentFolder.ToLowerInvariant()));

            object document = type == ContentModuleType.Units
                ? new
                {
                    formatVersion = 1,
                    id,
                    type = typeName,
                    @namespace = id,
                    title = title.Trim(),
                    description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    version = "1.0.0",
                    content = new { unitsDir = "Units" },
                    recruit = new { addsToPool = Array.Empty<string>() },
                }
                : new
                {
                    formatVersion = 1,
                    id,
                    type = typeName,
                    @namespace = id,
                    title = title.Trim(),
                    description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    version = "1.0.0",
                    content = new { buildingsDir = "Buildings" },
                };

            var path = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
            File.WriteAllText(
                path,
                JsonSerializer.Serialize(document, WriteOptions) + Environment.NewLine,
                Encoding.UTF8);
            return moduleRoot;
        }
        catch (Exception exception) when (exception is not EditorException)
        {
            try
            {
                if (Directory.Exists(moduleRoot))
                    Directory.Delete(moduleRoot, recursive: true);
            }
            catch (IOException)
            {
            }

            throw new EditorException($"Failed to create {typeName} module '{id}'.", exception);
        }
    }
}
