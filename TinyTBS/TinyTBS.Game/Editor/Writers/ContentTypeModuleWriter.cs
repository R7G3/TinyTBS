using System.Text;
using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Creates empty user units, buildings, or theme modules.</summary>
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

    public string CreateNew(
        ContentModuleType type,
        string moduleId,
        string title,
        string? description = null)
    {
        if (type is not (ContentModuleType.Units or ContentModuleType.Buildings or ContentModuleType.Theme))
            throw new EditorException("Unsupported module type for this wizard.");

        ContentModuleManifestParser.ValidateModuleId(moduleId.Trim());
        var id = moduleId.Trim();
        _userDataPaths.EnsureCreated();
        var moduleRoot = _files.Combine(_userDataPaths.Modules, id);
        if (Directory.Exists(moduleRoot))
            throw new EditorException($"User module '{id}' already exists.");

        var typeName = type switch
        {
            ContentModuleType.Units => "units",
            ContentModuleType.Buildings => "buildings",
            ContentModuleType.Theme => "theme",
            _ => throw new EditorException("Unsupported module type for this wizard."),
        };

        try
        {
            Directory.CreateDirectory(moduleRoot);

            object document;
            if (type == ContentModuleType.Units)
            {
                Directory.CreateDirectory(_files.Combine(moduleRoot, "Units"));
                Directory.CreateDirectory(_files.Combine(moduleRoot, "Resources", "Images", "units"));
                document = new
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
                };
            }
            else if (type == ContentModuleType.Buildings)
            {
                Directory.CreateDirectory(_files.Combine(moduleRoot, "Buildings"));
                Directory.CreateDirectory(_files.Combine(moduleRoot, "Resources", "Images", "buildings"));
                document = new
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
            }
            else
            {
                Directory.CreateDirectory(_files.Combine(moduleRoot, "Resources", "Images", "terrain"));
                Directory.CreateDirectory(_files.Combine(moduleRoot, "Resources", "Images", "misc"));
                document = new
                {
                    formatVersion = 1,
                    id,
                    type = typeName,
                    @namespace = id,
                    title = title.Trim(),
                    description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    version = "1.0.0",
                    content = new
                    {
                        terrainDir = "Resources/Images/terrain/",
                        gravestone = "Resources/Images/misc/gravestone.png",
                    },
                    remaps = new Dictionary<string, object>(),
                };
            }

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
