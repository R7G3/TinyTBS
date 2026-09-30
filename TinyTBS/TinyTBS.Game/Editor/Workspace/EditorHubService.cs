using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Editor.Bundles;
using TinyTBS.Game.Editor.Levels;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Editor.Units;
using TinyTBS.Game.Editor.ViewModels;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Maps;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Editor.Workspace;

/// <summary>
/// Library and documents behind the editor hub. The hub screen navigates; this type lists, opens,
/// copies and deletes modules, bundles and content files.
/// </summary>
public sealed class EditorHubService
{
    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;
    private readonly ContentModuleLibrary _modules;
    private readonly ContentBundleLibrary _bundles;
    private readonly BundleDocumentWriter _bundleWriter;
    private readonly EditorWorkspaceService _workspace;
    private readonly TinymodModuleExporter _exporter;

    public EditorHubService(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
        _modules = new ContentModuleLibrary(files, userDataPaths);
        _bundles = new ContentBundleLibrary(files, userDataPaths);
        _bundleWriter = new BundleDocumentWriter(files, userDataPaths);
        _workspace = new EditorWorkspaceService(files, userDataPaths);
        _exporter = new TinymodModuleExporter(files, userDataPaths);
    }

    public void Fill(EditorHubViewModel viewModel, EditorWorkspaceSession? session, string statusText)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        viewModel.Modules = _modules.ListEffectiveModules()
            .Select(module => new EditorModuleRowViewModel
            {
                ModuleId = module.ModuleId,
                Title = module.Title,
                Type = module.Type,
                Source = module.Source,
                ModuleRootPath = module.ModuleRootPath,
            })
            .ToArray();
        viewModel.Bundles = _bundles.ListEffectiveBundles()
            .Select(bundle => new EditorBundleRowViewModel
            {
                BundleId = bundle.BundleId,
                Title = bundle.Title,
                Source = bundle.Source,
                BundleFilePath = bundle.BundleFilePath,
            })
            .ToArray();
        viewModel.StatusText = statusText;
        viewModel.CanPublish = false;

        if (session is null)
        {
            viewModel.OpenModuleId = null;
            viewModel.OpenModuleTitle = null;
            viewModel.OpenModuleType = null;
            viewModel.CanCreateMap = false;
            viewModel.Maps = [];
            viewModel.Levels = [];
            viewModel.Units = [];
            viewModel.Buildings = [];
            return;
        }

        viewModel.OpenModuleId = session.ModuleId;
        viewModel.OpenModuleTitle = session.Title;
        viewModel.OpenModuleType = session.Type;
        viewModel.CanCreateMap = session.Type == ContentModuleType.Scenario;
        viewModel.Maps = _workspace.ListMapIds(session);
        viewModel.Levels = _workspace.ListLevelIds(session);
        viewModel.Units = _workspace.ListUnitIds(session);
        viewModel.Buildings = _workspace.ListBuildingIds(session);
    }

    public EditorWorkspaceSession OpenListedModule(EditorModuleRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (row.Source == ContentModuleSource.UserLibrary)
            return new EditorWorkspaceSession(row.ModuleId, row.ModuleRootPath, row.Type, row.Title);

        return _workspace.CopyToUserLibrary(row.ModuleId, row.ModuleRootPath, row.Type, row.Title);
    }

    public EditableBundleDocument CreateBundle()
    {
        var bundleId = _bundleWriter.AllocateUniqueBundleId("user_bundle");
        return EditableBundleDocument.CreateDefault(bundleId, _modules.ListEffectiveModules());
    }

    public EditableBundleDocument LoadUserBundle(string bundleId)
    {
        var path = UserBundlePath(bundleId);
        return EditableBundleDocument.Load(path, _files);
    }

    public EditableBundleDocument CopyBundledBundle(string bundleFilePath, string bundleId, string title)
    {
        var definition = ContentBundleLoader.Load(bundleFilePath, _files, ContentModuleSource.Bundled);
        var targetId = _bundleWriter.AllocateUniqueBundleId(bundleId);
        var copyTitle = targetId == bundleId ? title : title + " (copy)";
        _bundleWriter.CopyToUserLibrary(definition, targetId, copyTitle);
        return LoadUserBundle(targetId);
    }

    public bool DeleteBundle(string bundleId) => _bundleWriter.Delete(bundleId);

    public string ExportModule(EditorWorkspaceSession session) =>
        _exporter.ExportToDownloads(session.ModuleRootPath, session.ModuleId);

    public EditableLevelDocument CreateLevel(EditorWorkspaceSession session)
    {
        var maps = _workspace.ListMapIds(session);
        var mapId = maps.Count > 0 ? maps[0] : "map";
        var levelId = _workspace.AllocateLevelId(session, mapId);
        return EditableLevelDocument.CreateDefault(levelId, title: levelId, mapId: mapId);
    }

    public EditableUnitDocument CreateUnit(EditorWorkspaceSession session) =>
        EditableUnitDocument.CreateDefault(_workspace.AllocateUnitId(session, "unit"));

    public EditableBuildingDocument CreateBuilding(EditorWorkspaceSession session) =>
        EditableBuildingDocument.CreateDefault(_workspace.AllocateBuildingId(session, "building"));

    public EditableMapDocument LoadMap(EditorWorkspaceSession session, string mapId)
    {
        var definition = MapFolderLoader.Load(_workspace.MapFolder(session, mapId), _files);
        return EditableMapDocument.FromDefinition(definition);
    }

    public EditableLevelDocument LoadLevel(EditorWorkspaceSession session, string levelId) =>
        EditableLevelDocument.Load(_workspace.LevelFolder(session, levelId), _files);

    public EditableUnitDocument LoadUnit(EditorWorkspaceSession session, string unitId) =>
        EditableUnitDocument.Load(_workspace.UnitFile(session, unitId), _files);

    public EditableBuildingDocument LoadBuilding(EditorWorkspaceSession session, string buildingId) =>
        EditableBuildingDocument.Load(_workspace.BuildingFile(session, buildingId), _files);

    public void DeleteMap(EditorWorkspaceSession session, string mapId) => _workspace.DeleteMap(session, mapId);

    public void DeleteLevel(EditorWorkspaceSession session, string levelId) => _workspace.DeleteLevel(session, levelId);

    public void DeleteUnit(EditorWorkspaceSession session, string unitId) => _workspace.DeleteUnit(session, unitId);

    public void DeleteBuilding(EditorWorkspaceSession session, string buildingId) =>
        _workspace.DeleteBuilding(session, buildingId);

    private string UserBundlePath(string bundleId) =>
        _files.Combine(_userDataPaths.Bundles, bundleId + ContentBundleFiles.BundleJsonExtension);
}
