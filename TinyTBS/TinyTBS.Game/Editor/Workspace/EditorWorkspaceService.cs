using System.Text.Json;
using System.Text.Json.Nodes;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Editor.Workspace;

/// <summary>
/// File operations behind the editor screens: listing, locating, allocating and deleting maps / levels /
/// units / buildings of an open module, allocating module ids, and copying a bundled module into the user library.
/// </summary>
public sealed class EditorWorkspaceService
{
    private const string MapsFolderName = "Maps";
    private const string LevelsFolderName = "Levels";
    private const string UnitsFolderName = "Units";
    private const string BuildingsFolderName = "Buildings";
    private const string JsonExtension = ".json";

    private static readonly JsonSerializerOptions ManifestWriteOptions = new() { WriteIndented = true };

    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public EditorWorkspaceService(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    public IReadOnlyList<string> ListMapIds(EditorWorkspaceSession session) =>
        session.Type == ContentModuleType.Scenario
            ? ListFolderNames(_files.Combine(session.ModuleRootPath, MapsFolderName))
            : [];

    public IReadOnlyList<string> ListLevelIds(EditorWorkspaceSession session) =>
        session.Type == ContentModuleType.Scenario
            ? ListFolderNames(_files.Combine(session.ModuleRootPath, LevelsFolderName))
            : [];

    public IReadOnlyList<string> ListUnitIds(EditorWorkspaceSession session) =>
        session.Type == ContentModuleType.Units
            ? ListJsonNames(_files.Combine(session.ModuleRootPath, UnitsFolderName))
            : [];

    public IReadOnlyList<string> ListBuildingIds(EditorWorkspaceSession session) =>
        session.Type == ContentModuleType.Buildings
            ? ListJsonNames(_files.Combine(session.ModuleRootPath, BuildingsFolderName))
            : [];

    public string MapFolder(EditorWorkspaceSession session, string mapId) =>
        _files.Combine(session.ModuleRootPath, MapsFolderName, mapId);

    public string LevelFolder(EditorWorkspaceSession session, string levelId) =>
        _files.Combine(session.ModuleRootPath, LevelsFolderName, levelId);

    public string UnitFile(EditorWorkspaceSession session, string unitId) =>
        _files.Combine(session.ModuleRootPath, UnitsFolderName, unitId + JsonExtension);

    public string BuildingFile(EditorWorkspaceSession session, string buildingId) =>
        _files.Combine(session.ModuleRootPath, BuildingsFolderName, buildingId + JsonExtension);

    public string AllocateMapId(EditorWorkspaceSession session, string stem) =>
        EditorIds.AllocateUnique(stem, candidate => _files.DirectoryExists(MapFolder(session, candidate)), "map");

    public string AllocateLevelId(EditorWorkspaceSession session, string stem) =>
        EditorIds.AllocateUnique(stem, candidate => _files.DirectoryExists(LevelFolder(session, candidate)), "level");

    public string AllocateUnitId(EditorWorkspaceSession session, string stem) =>
        EditorIds.AllocateUnique(stem, candidate => _files.Exists(UnitFile(session, candidate)), "unit");

    public string AllocateBuildingId(EditorWorkspaceSession session, string stem) =>
        EditorIds.AllocateUnique(stem, candidate => _files.Exists(BuildingFile(session, candidate)), "building");

    /// <summary>Picks <paramref name="stem"/>, <c>{stem}_2</c>, … not yet used in the user module library.</summary>
    public string AllocateModuleId(string stem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stem);
        ContentModuleManifestParser.ValidateModuleId(stem);
        _userDataPaths.EnsureCreated();
        return EditorIds.AllocateUnique(stem, IsUserModuleIdTaken, "module");
    }

    public bool IsUserModuleIdTaken(string moduleId) =>
        _files.DirectoryExists(_files.Combine(_userDataPaths.Modules, moduleId));

    public void DeleteMap(EditorWorkspaceSession session, string mapId) =>
        DeleteFolder(MapFolder(session, mapId));

    public void DeleteLevel(EditorWorkspaceSession session, string levelId) =>
        DeleteFolder(LevelFolder(session, levelId));

    /// <summary>Deletes the unit file and drops it from the module's recruit pool.</summary>
    public void DeleteUnit(EditorWorkspaceSession session, string unitId)
    {
        var path = UnitFile(session, unitId);
        if (_files.Exists(path))
            _files.DeleteFile(path);

        RemoveFromRecruitPool(session.ModuleRootPath, unitId);
    }

    public void DeleteBuilding(EditorWorkspaceSession session, string buildingId)
    {
        var path = BuildingFile(session, buildingId);
        if (_files.Exists(path))
            _files.DeleteFile(path);
    }

    /// <summary>
    /// Copies a bundled module into the user library — as <c>{id}_copy</c> (and a "(copy)" title) when the id is
    /// already taken there — and returns an edit session on the copy.
    /// </summary>
    public EditorWorkspaceSession CopyToUserLibrary(
        string moduleId,
        string moduleRootPath,
        ContentModuleType type,
        string title)
    {
        ContentModuleManifestParser.ValidateModuleId(moduleId);
        var targetId = IsUserModuleIdTaken(moduleId) ? AllocateModuleId(moduleId + "_copy") : moduleId;
        var destination = ModuleDirectoryCopier.CopyToUserLibrary(moduleRootPath, targetId, _files, _userDataPaths);
        if (string.Equals(targetId, moduleId, StringComparison.Ordinal))
            return new EditorWorkspaceSession(targetId, destination, type, title);

        var copyTitle = title + " (copy)";
        ModuleManifestIdRewriter.RewriteIdentity(destination, targetId, _files, title: copyTitle);
        return new EditorWorkspaceSession(targetId, destination, type, copyTitle);
    }

    private IReadOnlyList<string> ListFolderNames(string root)
    {
        if (!_files.DirectoryExists(root))
            return [];

        return _files.EnumerateDirectories(root)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => name.Length > 0)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<string> ListJsonNames(string folder)
    {
        if (!_files.DirectoryExists(folder))
            return [];

        return _files.EnumerateFiles(folder, "*" + JsonExtension)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(name => name.Length > 0)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void DeleteFolder(string path)
    {
        if (_files.DirectoryExists(path))
            _files.DeleteDirectory(path);
    }

    private void RemoveFromRecruitPool(string moduleRoot, string localUnitId)
    {
        var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!_files.Exists(moduleJsonPath))
            return;

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(_files.ReadAllText(moduleJsonPath));
        }
        catch (JsonException)
        {
            return;
        }

        if (root is not JsonObject rootObject
            || rootObject["recruit"] is not JsonObject recruitObject
            || recruitObject["addsToPool"] is not JsonArray existing)
        {
            return;
        }

        var contentNamespace = rootObject["namespace"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(contentNamespace))
            contentNamespace = rootObject["id"]?.GetValue<string>() ?? string.Empty;

        var fullId = string.IsNullOrWhiteSpace(contentNamespace)
            ? localUnitId
            : contentNamespace + "/" + localUnitId;

        var pool = new JsonArray();
        foreach (var entry in existing)
        {
            var value = entry?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(value)
                || string.Equals(value, fullId, StringComparison.Ordinal)
                || string.Equals(value, localUnitId, StringComparison.Ordinal))
            {
                continue;
            }

            pool.Add(value);
        }

        recruitObject["addsToPool"] = pool;
        _files.WriteAllText(moduleJsonPath, rootObject.ToJsonString(ManifestWriteOptions) + Environment.NewLine);
    }
}
