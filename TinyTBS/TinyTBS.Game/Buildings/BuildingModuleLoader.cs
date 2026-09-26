using TinyTBS.Engine.IO;
using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Buildings;

/// <summary>Loads a buildings module folder (<c>module.json</c> + <c>Buildings/*.json</c>).</summary>
public static class BuildingModuleLoader
{
    public static BuildingModuleDefinition Load(string moduleRoot, IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
        ArgumentNullException.ThrowIfNull(files);

        if (!Directory.Exists(moduleRoot))
            throw new BuildingLoadException($"Buildings module folder not found: {moduleRoot}");

        var moduleJsonPath = files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!files.Exists(moduleJsonPath))
            throw new BuildingLoadException($"Missing {ContentModuleFiles.ModuleJsonFileName} in {moduleRoot}");

        string moduleId;
        string contentNamespace;
        string title;
        string version;
        string buildingsDir;
        using (var moduleStream = files.OpenRead(moduleJsonPath))
        {
            (moduleId, contentNamespace, title, version, buildingsDir) =
                BuildingJsonParser.ParseModuleManifest(moduleStream);
        }

        var buildingsDirectory = files.Combine(moduleRoot, buildingsDir.TrimEnd('/', '\\'));
        if (!Directory.Exists(buildingsDirectory))
            throw new BuildingLoadException($"Buildings directory not found: {buildingsDirectory}");

        var buildingsById = new Dictionary<ContentId, BuildingDefinition>();
        foreach (var buildingFilePath in Directory.EnumerateFiles(
                     buildingsDirectory,
                     "*.json",
                     SearchOption.TopDirectoryOnly))
        {
            using var buildingStream = files.OpenRead(buildingFilePath);
            var buildingDefinition = BuildingJsonParser.ParseBuilding(buildingStream, contentNamespace, moduleRoot);
            if (!buildingsById.TryAdd(buildingDefinition.ContentId, buildingDefinition))
            {
                throw new BuildingLoadException(
                    $"Duplicate building id '{buildingDefinition.ContentId.Full}' in module '{moduleId}'.");
            }
        }

        if (buildingsById.Count == 0)
            throw new BuildingLoadException($"Buildings module '{moduleId}' contains no building JSON files.");

        return new BuildingModuleDefinition
        {
            ModuleId = moduleId,
            ContentNamespace = contentNamespace,
            Title = title,
            Version = version,
            BuildingsById = buildingsById,
            ModuleRootPath = moduleRoot,
        };
    }
}
