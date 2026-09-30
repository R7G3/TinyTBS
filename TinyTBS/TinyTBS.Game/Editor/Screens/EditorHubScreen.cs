using Microsoft.Xna.Framework;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Editor.Bundles;
using TinyTBS.Game.Editor.Levels;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Units;
using TinyTBS.Game.Editor.ViewModels;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Maps;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Screens;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>
/// Editor hub: modules, CoW duplicate, open session, maps/levels/campaign/units/buildings.
/// </summary>
public sealed class EditorHubScreen : MenuScreen
{
    private readonly EditorHubViewModel _viewModel = new();
    private readonly EditorHubView _view = new();

    private ContentModuleLibrary? _moduleLibrary;
    private ContentBundleLibrary? _bundleLibrary;
    private BundleDocumentWriter? _bundleWriter;
    private EditorWorkspaceService? _workspace;
    private EditorWorkspaceSession? _session;

    /// <summary>
    /// After ReplaceScreen from a child (e.g. New Scenario Back), ignore exit commands,
    /// Confirm, and Gum clicks until pointer + exit/confirm keys are released — otherwise
    /// Hub Back under the same cursor (or leftover Confirm) jumps to the main menu.
    /// </summary>
    private bool _suppressInputUntilIdle;

    public EditorHubScreen(GameMain game, IAssetResolver assets)
        : this(game, assets, session: null)
    {
    }

    public EditorHubScreen(GameMain game, IAssetResolver assets, EditorWorkspaceSession? session)
        : base(game, assets)
    {
        _session = session;
    }

    private EditorWorkspaceService Workspace =>
        _workspace ?? throw new InvalidOperationException("Editor hub is not loaded.");

    protected override void OnLoad()
    {
        TinyGame.UserDataPaths.EnsureCreated();
        _moduleLibrary = new ContentModuleLibrary(TinyGame.Files, TinyGame.UserDataPaths);
        _bundleLibrary = new ContentBundleLibrary(TinyGame.Files, TinyGame.UserDataPaths);
        _bundleWriter = new BundleDocumentWriter(TinyGame.Files, TinyGame.UserDataPaths);
        _workspace = new EditorWorkspaceService(TinyGame.Files, TinyGame.UserDataPaths);

        BeginInputSuppress();
        RefreshLibrary(
            _session is null
                ? "Create a scenario or Confirm a bundled module to copy into your library."
                : $"Editing '{_session.ModuleId}'. Content below.");
    }

    protected override void OnUnload()
    {
        _view.Clear();
        _moduleLibrary = null;
        _bundleLibrary = null;
        _bundleWriter = null;
        _workspace = null;
    }

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        _view.ApplyResponsiveLayout();

        if (_suppressInputUntilIdle)
        {
            if (IsIngressBusy(TinyGame.Commands, TinyGame.Pointer))
                return;

            EndInputSuppress();
        }

        var detailWasOpen = _view.IsAnyDetailOpen;
        _view.HandleInput(TinyGame.Commands, elapsedSeconds);

        if (!WasLeavePressed())
            return;

        if (detailWasOpen || _view.IsAnyDetailOpen)
        {
            _view.TryCloseAnyDetail();
            return;
        }

        HandleBack();
    }

    private void BeginInputSuppress()
    {
        _suppressInputUntilIdle = true;
        _view.SuppressActivations = true;
    }

    private void EndInputSuppress()
    {
        _suppressInputUntilIdle = false;
        _view.SuppressActivations = false;
    }

    private static bool IsIngressBusy(IGameCommandSource commands, IPointerSource pointer) =>
        pointer.IsPrimaryDown
        || commands.IsPressed(GameCommand.Confirm)
        || commands.IsPressed(GameCommand.Back)
        || commands.IsPressed(GameCommand.Cancel)
        || commands.IsPressed(GameCommand.Info)
        || commands.IsPressed(GameCommand.Pause);

    private void RefreshLibrary(string statusText)
    {
        ArgumentNullException.ThrowIfNull(_moduleLibrary);
        ArgumentNullException.ThrowIfNull(_bundleLibrary);

        var modules = _moduleLibrary.ListEffectiveModules()
            .Select(module => new EditorModuleRowViewModel
            {
                ModuleId = module.ModuleId,
                Title = module.Title,
                Type = module.Type,
                Source = module.Source,
                ModuleRootPath = module.ModuleRootPath,
            })
            .ToArray();

        var bundles = _bundleLibrary.ListEffectiveBundles()
            .Select(bundle => new EditorBundleRowViewModel
            {
                BundleId = bundle.BundleId,
                Title = bundle.Title,
                Source = bundle.Source,
                BundleFilePath = bundle.BundleFilePath,
            })
            .ToArray();

        _viewModel.Modules = modules;
        _viewModel.Bundles = bundles;
        _viewModel.StatusText = statusText;
        _viewModel.CanPublish = false;
        SyncOpenFields();
        _view.Build(
            _viewModel,
            onNewScenario: OpenNewScenarioWizard,
            onNewUnitsModule: () => OpenNewContentTypeWizard(ContentModuleType.Units),
            onNewBuildingsModule: () => OpenNewContentTypeWizard(ContentModuleType.Buildings),
            onNewThemeModule: () => OpenNewContentTypeWizard(ContentModuleType.Theme),
            onExportModule: ExportOpenModule,
            onNewMap: OpenNewMapWizard,
            onNewLevel: OpenNewLevelWizard,
            onEditCampaign: OpenCampaignEditor,
            onNewUnit: OpenNewUnit,
            onNewBuilding: OpenNewBuilding,
            onEditTheme: OpenThemeEditor,
            onNewBundle: OpenNewBundle,
            onActivateModule: ActivateModule,
            onActivateBundle: ActivateBundle,
            onOpenMap: OpenExistingMap,
            onDeleteMap: DeleteMap,
            onOpenLevel: OpenExistingLevel,
            onDeleteLevel: DeleteLevel,
            onOpenUnit: OpenExistingUnit,
            onDeleteUnit: DeleteUnit,
            onOpenBuilding: OpenExistingBuilding,
            onDeleteBuilding: DeleteBuilding,
            onOpenBundle: OpenExistingBundle,
            onDeleteBundle: DeleteBundle,
            onCloseModule: CloseModule,
            onBack: HandleBack);
    }

    private void SyncOpenFields()
    {
        if (_session is null)
        {
            _viewModel.OpenModuleId = null;
            _viewModel.OpenModuleTitle = null;
            _viewModel.OpenModuleType = null;
            _viewModel.CanCreateMap = false;
            _viewModel.Maps = [];
            _viewModel.Levels = [];
            _viewModel.Units = [];
            _viewModel.Buildings = [];
            return;
        }

        _viewModel.OpenModuleId = _session.ModuleId;
        _viewModel.OpenModuleTitle = _session.Title;
        _viewModel.OpenModuleType = _session.Type;
        _viewModel.CanCreateMap = _session.Type == ContentModuleType.Scenario;
        _viewModel.Maps = Workspace.ListMapIds(_session);
        _viewModel.Levels = Workspace.ListLevelIds(_session);
        _viewModel.Units = Workspace.ListUnitIds(_session);
        _viewModel.Buildings = Workspace.ListBuildingIds(_session);
    }

    private void ShowStatus(string statusText)
    {
        _viewModel.StatusText = statusText;
        _view.SyncStatus(_viewModel);
    }

    private void OpenNewScenarioWizard() =>
        ScreenManager.ReplaceScreen(new EditorNewScenarioScreen(TinyGame, Assets, _session));

    private void OpenNewContentTypeWizard(ContentModuleType type) =>
        ScreenManager.ReplaceScreen(new EditorNewContentTypeModuleScreen(TinyGame, Assets, _session, type));

    private void OpenThemeEditor()
    {
        if (_session is null || _session.Type != ContentModuleType.Theme)
            return;

        ScreenManager.ReplaceScreen(new EditorThemeEditScreen(TinyGame, Assets, _session));
    }

    private void OpenNewBundle()
    {
        ArgumentNullException.ThrowIfNull(_moduleLibrary);
        ArgumentNullException.ThrowIfNull(_bundleWriter);

        var modules = _moduleLibrary.ListEffectiveModules();
        var bundleId = _bundleWriter.AllocateUniqueBundleId("user_bundle");
        var document = EditableBundleDocument.CreateDefault(bundleId, modules);
        ScreenManager.ReplaceScreen(
            new EditorBundleEditScreen(TinyGame, Assets, _session, document, isNew: true));
    }

    private void OpenExistingBundle(string bundleId)
    {
        try
        {
            var path = TinyGame.Files.Combine(
                TinyGame.UserDataPaths.Bundles,
                bundleId + ContentBundleFiles.BundleJsonExtension);
            var document = EditableBundleDocument.Load(path, TinyGame.Files);
            ScreenManager.ReplaceScreen(
                new EditorBundleEditScreen(TinyGame, Assets, _session, document, isNew: false));
        }
        catch (Exception exception)
        {
            ShowStatus("Open bundle failed: " + exception.Message);
        }
    }

    private void DeleteBundle(string bundleId)
    {
        ArgumentNullException.ThrowIfNull(_bundleWriter);

        try
        {
            if (_bundleWriter.Delete(bundleId))
                RefreshLibrary($"Deleted bundle '{bundleId}'.");
            else
                ShowStatus($"Bundle '{bundleId}' was already gone.");
        }
        catch (Exception exception) when (exception is EditorException or IOException or UnauthorizedAccessException)
        {
            ShowStatus("Delete bundle failed: " + exception.Message);
        }
    }

    private void ActivateBundle(EditorBundleRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(_bundleWriter);

        try
        {
            if (row.Source == ContentModuleSource.UserLibrary)
            {
                OpenExistingBundle(row.BundleId);
                return;
            }

            var definition = ContentBundleLoader.Load(
                row.BundleFilePath,
                TinyGame.Files,
                ContentModuleSource.Bundled);
            var targetId = _bundleWriter.AllocateUniqueBundleId(row.BundleId);
            var title = targetId == row.BundleId ? row.Title : row.Title + " (copy)";
            _bundleWriter.CopyToUserLibrary(definition, targetId, title);
            var document = EditableBundleDocument.Load(
                TinyGame.Files.Combine(
                    TinyGame.UserDataPaths.Bundles,
                    targetId + ContentBundleFiles.BundleJsonExtension),
                TinyGame.Files);
            ScreenManager.ReplaceScreen(
                new EditorBundleEditScreen(TinyGame, Assets, _session, document, isNew: false));
        }
        catch (Exception exception) when (exception is EditorException or ContentBundleException or IOException)
        {
            ShowStatus("Open/Duplicate bundle failed: " + exception.Message);
        }
    }

    private void ExportOpenModule()
    {
        if (_session is null)
            return;

        try
        {
            var exporter = new TinymodModuleExporter(TinyGame.Files, TinyGame.UserDataPaths);
            var zipPath = exporter.ExportToDownloads(_session.ModuleRootPath, _session.ModuleId);
            RefreshLibrary($"Exported → {zipPath}");
        }
        catch (Exception exception) when (exception is TinymodExportException or IOException)
        {
            ShowStatus("Export failed: " + exception.Message);
        }
    }

    private void OpenNewMapWizard()
    {
        if (_session is null || _session.Type != ContentModuleType.Scenario)
            return;

        ScreenManager.ReplaceScreen(new EditorNewMapScreen(TinyGame, Assets, _session));
    }

    private void OpenNewLevelWizard()
    {
        if (_session is null || _session.Type != ContentModuleType.Scenario)
            return;

        var maps = Workspace.ListMapIds(_session);
        var mapId = maps.Count > 0 ? maps[0] : "map";
        var levelId = Workspace.AllocateLevelId(_session, mapId);
        var document = EditableLevelDocument.CreateDefault(levelId, title: levelId, mapId: mapId);
        ScreenManager.ReplaceScreen(
            new EditorLevelEditScreen(TinyGame, Assets, _session, document, isNew: true));
    }

    private void OpenCampaignEditor()
    {
        if (_session is null || _session.Type != ContentModuleType.Scenario)
            return;

        ScreenManager.ReplaceScreen(new EditorCampaignEditScreen(TinyGame, Assets, _session));
    }

    private void OpenNewUnit()
    {
        if (_session is null || _session.Type != ContentModuleType.Units)
            return;

        var document = EditableUnitDocument.CreateDefault(Workspace.AllocateUnitId(_session, "unit"));
        ScreenManager.ReplaceScreen(
            new EditorUnitEditScreen(TinyGame, Assets, _session, document, isNew: true));
    }

    private void OpenNewBuilding()
    {
        if (_session is null || _session.Type != ContentModuleType.Buildings)
            return;

        var document = EditableBuildingDocument.CreateDefault(Workspace.AllocateBuildingId(_session, "building"));
        ScreenManager.ReplaceScreen(
            new EditorBuildingEditScreen(TinyGame, Assets, _session, document, isNew: true));
    }

    private void OpenExistingMap(string mapId)
    {
        if (_session is null)
            return;

        try
        {
            var definition = MapFolderLoader.Load(Workspace.MapFolder(_session, mapId), TinyGame.Files);
            var document = EditableMapDocument.FromDefinition(definition);
            ScreenManager.ReplaceScreen(
                new EditorMapPaintScreen(TinyGame, Assets, _session, document, isNewMap: false));
        }
        catch (Exception exception)
        {
            ShowStatus("Open map failed: " + exception.Message);
        }
    }

    private void DeleteMap(string mapId)
    {
        if (_session is null)
            return;

        try
        {
            Workspace.DeleteMap(_session, mapId);
            RefreshLibrary($"Deleted map '{mapId}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowStatus("Delete map failed: " + exception.Message);
        }
    }

    private void OpenExistingLevel(string levelId)
    {
        if (_session is null)
            return;

        try
        {
            var document = EditableLevelDocument.Load(Workspace.LevelFolder(_session, levelId), TinyGame.Files);
            ScreenManager.ReplaceScreen(
                new EditorLevelEditScreen(TinyGame, Assets, _session, document, isNew: false));
        }
        catch (Exception exception)
        {
            ShowStatus("Open level failed: " + exception.Message);
        }
    }

    private void DeleteLevel(string levelId)
    {
        if (_session is null)
            return;

        try
        {
            Workspace.DeleteLevel(_session, levelId);
            RefreshLibrary($"Deleted level '{levelId}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowStatus("Delete level failed: " + exception.Message);
        }
    }

    private void OpenExistingUnit(string unitId)
    {
        if (_session is null)
            return;

        try
        {
            var document = EditableUnitDocument.Load(Workspace.UnitFile(_session, unitId), TinyGame.Files);
            ScreenManager.ReplaceScreen(
                new EditorUnitEditScreen(TinyGame, Assets, _session, document, isNew: false));
        }
        catch (Exception exception)
        {
            ShowStatus("Open unit failed: " + exception.Message);
        }
    }

    private void DeleteUnit(string unitId)
    {
        if (_session is null)
            return;

        try
        {
            Workspace.DeleteUnit(_session, unitId);
            RefreshLibrary($"Deleted unit '{unitId}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or EditorException)
        {
            ShowStatus("Delete unit failed: " + exception.Message);
        }
    }

    private void OpenExistingBuilding(string buildingId)
    {
        if (_session is null)
            return;

        try
        {
            var document = EditableBuildingDocument.Load(Workspace.BuildingFile(_session, buildingId), TinyGame.Files);
            ScreenManager.ReplaceScreen(
                new EditorBuildingEditScreen(TinyGame, Assets, _session, document, isNew: false));
        }
        catch (Exception exception)
        {
            ShowStatus("Open building failed: " + exception.Message);
        }
    }

    private void DeleteBuilding(string buildingId)
    {
        if (_session is null)
            return;

        try
        {
            Workspace.DeleteBuilding(_session, buildingId);
            RefreshLibrary($"Deleted building '{buildingId}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowStatus("Delete building failed: " + exception.Message);
        }
    }

    private void ActivateModule(EditorModuleRowViewModel row)
    {
        try
        {
            if (row.Source == ContentModuleSource.UserLibrary)
            {
                _session = new EditorWorkspaceSession(row.ModuleId, row.ModuleRootPath, row.Type, row.Title);
                RefreshLibrary($"Opened '{row.ModuleId}'.");
                return;
            }

            _session = Workspace.CopyToUserLibrary(row.ModuleId, row.ModuleRootPath, row.Type, row.Title);
            RefreshLibrary($"Duplicated '{row.ModuleId}' → '{_session.ModuleId}' and opened.");
        }
        catch (Exception exception) when (exception is EditorException or TinymodInstallException or IOException)
        {
            ShowStatus("Open/Duplicate failed: " + exception.Message);
        }
    }

    private void CloseModule()
    {
        _session = null;
        RefreshLibrary("Module closed.");
    }

    /// <summary>
    /// Open module → close session (hub root). No module → leave editor to main menu.
    /// </summary>
    private void HandleBack()
    {
        if (_session is not null)
        {
            CloseModule();
            return;
        }

        GoToMainMenu();
    }

    private void GoToMainMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, Assets));
}
