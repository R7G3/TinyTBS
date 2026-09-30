using System.Text;
using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Creates an empty user scenario module folder (module.json + Maps/ + Levels/).</summary>
public sealed class ScenarioModuleWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public ScenarioModuleWriter(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    /// <summary>
    /// Creates <c>{Modules}/{moduleId}/</c> with vanilla composition defaults and empty Maps/Levels.
    /// </summary>
    public string CreateNew(
        string moduleId,
        ref string title,
        string? contentNamespace = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);

        ContentModuleManifestParser.ValidateModuleId(moduleId);
        var id = moduleId;
        title = SavedUserText.Or(title, id);
        var savedDescription = SavedUserText.Optional(description);
        var moduleNamespace = string.IsNullOrWhiteSpace(contentNamespace) ? id : contentNamespace;

        _userDataPaths.EnsureCreated();
        var moduleRoot = _files.Combine(_userDataPaths.Modules, id);
        if (_files.DirectoryExists(moduleRoot))
            throw new EditorException($"User module '{id}' already exists.");

        try
        {
            _files.CreateDirectory(moduleRoot);
            _files.CreateDirectory(_files.Combine(moduleRoot, "Maps"));
            _files.CreateDirectory(_files.Combine(moduleRoot, "Levels"));

            var document = new ScenarioModuleJsonDto
            {
                FormatVersion = 1,
                Id = id,
                Type = ContentModuleTypeIds.Scenario,
                Namespace = moduleNamespace,
                Title = title,
                Description = savedDescription,
                Version = "1.0.0",
                Defaults = new ScenarioDefaultsDto
                {
                    Units = [VanillaContentIds.UnitsModuleId],
                    Buildings = [VanillaContentIds.BuildingsModuleId],
                    Theme = VanillaContentIds.ThemeModuleId,
                },
                Requires = new ScenarioRequiresDto
                {
                    Units = [VanillaContentIds.UnitsModuleId],
                    Buildings = [VanillaContentIds.BuildingsModuleId],
                },
            };

            var json = JsonSerializer.Serialize(document, WriteOptions);
            var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
            _files.WriteAllText(moduleJsonPath, json + Environment.NewLine, Encoding.UTF8);
            return moduleRoot;
        }
        catch (Exception exception) when (exception is not EditorException)
        {
            TryDeleteDirectory(moduleRoot);
            throw new EditorException($"Failed to create scenario module '{id}'.", exception);
        }
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (_files.DirectoryExists(path))
                _files.DeleteDirectory(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
