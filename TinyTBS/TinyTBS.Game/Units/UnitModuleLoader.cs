using TinyTBS.Engine.IO;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Units;

/// <summary>Loads a units module folder (<c>module.json</c> + <c>Units/*.json</c>).</summary>
public static class UnitModuleLoader
{
    public const string ModuleJsonFileName = "module.json";

    public static UnitModuleDefinition Load(string moduleRoot, IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
        ArgumentNullException.ThrowIfNull(files);

        if (!Directory.Exists(moduleRoot))
            throw new UnitLoadException($"Units module folder not found: {moduleRoot}");

        var moduleJsonPath = files.Combine(moduleRoot, ModuleJsonFileName);
        if (!files.Exists(moduleJsonPath))
            throw new UnitLoadException($"Missing {ModuleJsonFileName} in {moduleRoot}");

        string moduleId;
        string contentNamespace;
        string title;
        string version;
        string unitsDir;
        IReadOnlyList<ContentId> recruitPool;
        using (var moduleStream = files.OpenRead(moduleJsonPath))
        {
            (moduleId, contentNamespace, title, version, unitsDir, recruitPool) =
                UnitJsonParser.ParseModuleManifest(moduleStream);
        }

        var unitsDirectory = files.Combine(moduleRoot, unitsDir.TrimEnd('/', '\\'));
        if (!Directory.Exists(unitsDirectory))
            throw new UnitLoadException($"Units directory not found: {unitsDirectory}");

        var unitsById = new Dictionary<ContentId, UnitDefinition>();
        foreach (var unitFilePath in Directory.EnumerateFiles(unitsDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            using var unitStream = files.OpenRead(unitFilePath);
            var unitDefinition = UnitJsonParser.ParseUnit(unitStream, contentNamespace);
            if (!unitsById.TryAdd(unitDefinition.ContentId, unitDefinition))
            {
                throw new UnitLoadException(
                    $"Duplicate unit id '{unitDefinition.ContentId.Full}' in module '{moduleId}'.");
            }
        }

        if (unitsById.Count == 0)
            throw new UnitLoadException($"Units module '{moduleId}' contains no unit JSON files.");

        foreach (var recruitId in recruitPool)
        {
            if (!unitsById.ContainsKey(recruitId))
            {
                throw new UnitLoadException(
                    $"Recruit pool id '{recruitId.Full}' is not defined in module '{moduleId}'.");
            }
        }

        return new UnitModuleDefinition
        {
            ModuleId = moduleId,
            ContentNamespace = contentNamespace,
            Title = title,
            Version = version,
            RecruitPool = recruitPool,
            UnitsById = unitsById,
            ModuleRootPath = moduleRoot,
        };
    }
}
