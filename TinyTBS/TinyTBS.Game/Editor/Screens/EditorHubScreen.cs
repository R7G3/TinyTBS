using Microsoft.Xna.Framework;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.ViewModels;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Screens;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>
/// Editor hub glue: input, back, and navigation. Listing, copy, open and delete go through
/// <see cref="EditorHubService"/>.
/// </summary>
public sealed class EditorHubScreen : MenuScreen
{
    private readonly EditorHubViewModel _viewModel = new();
    private readonly EditorHubView _view = new();

    private EditorHubService? _hub;
    private EditorWorkspaceSession? _session;

    /// <summary>
    /// After ReplaceScreen from a child (e.g. New Scenario Back), ignore exit commands,
    /// Confirm, and Gum clicks until pointer + exit/confirm keys are released — otherwise
    /// Hub Back under the same cursor (or leftover Confirm) jumps to the main menu.
    /// </summary>
    private bool _suppressInputUntilIdle;

    public EditorHubScreen(GameMain game)
        : this(game, session: null)
    {
    }

    public EditorHubScreen(GameMain game, EditorWorkspaceSession? session)
        : base(game)
    {
        _session = session;
    }

    private EditorHubService Hub =>
        _hub ?? throw new InvalidOperationException("Editor hub is not loaded.");

    protected override void OnLoad()
    {
        TinyGame.UserDataPaths.EnsureCreated();
        _hub = new EditorHubService(TinyGame.Files, TinyGame.UserDataPaths);

        BeginInputSuppress();
        RefreshLibrary(
            _session is null
                ? "Create a scenario or Confirm a bundled module to copy into your library."
                : $"Editing '{_session.ModuleId}'. Content below.");
    }

    protected override void OnUnload()
    {
        _view.Clear();
        _hub = null;
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
        Hub.Fill(_viewModel, _session, statusText);
        _view.Build(
            _viewModel,
            onNewScenario: () => Navigator.ToEditorNewScenario(_session),
            onNewUnitsModule: () => Navigator.ToEditorNewContentType(_session, ContentModuleType.Units),
            onNewBuildingsModule: () => Navigator.ToEditorNewContentType(_session, ContentModuleType.Buildings),
            onNewThemeModule: () => Navigator.ToEditorNewContentType(_session, ContentModuleType.Theme),
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

    private void ShowStatus(string statusText)
    {
        _viewModel.StatusText = statusText;
        _view.SyncStatus(_viewModel);
    }

    private void OpenThemeEditor()
    {
        if (_session is null || _session.Type != ContentModuleType.Theme)
            return;

        Navigator.ToEditorTheme(_session);
    }

    private void OpenNewBundle()
    {
        var document = Hub.CreateBundle();
        Navigator.ToEditorBundle(_session, document, isNew: true);
    }

    private void OpenExistingBundle(string bundleId)
    {
        try
        {
            var document = Hub.LoadUserBundle(bundleId);
            Navigator.ToEditorBundle(_session, document, isNew: false);
        }
        catch (Exception exception)
        {
            ShowStatus("Open bundle failed: " + exception.Message);
        }
    }

    private void DeleteBundle(string bundleId)
    {
        try
        {
            if (Hub.DeleteBundle(bundleId))
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
        try
        {
            if (row.Source == ContentModuleSource.UserLibrary)
            {
                OpenExistingBundle(row.BundleId);
                return;
            }

            var document = Hub.CopyBundledBundle(row.BundleFilePath, row.BundleId, row.Title);
            Navigator.ToEditorBundle(_session, document, isNew: false);
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
            var zipPath = Hub.ExportModule(_session);
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

        Navigator.ToEditorNewMap(_session);
    }

    private void OpenNewLevelWizard()
    {
        if (_session is null || _session.Type != ContentModuleType.Scenario)
            return;

        var document = Hub.CreateLevel(_session);
        Navigator.ToEditorLevel(_session, document, isNew: true);
    }

    private void OpenCampaignEditor()
    {
        if (_session is null || _session.Type != ContentModuleType.Scenario)
            return;

        Navigator.ToEditorCampaign(_session);
    }

    private void OpenNewUnit()
    {
        if (_session is null || _session.Type != ContentModuleType.Units)
            return;

        var document = Hub.CreateUnit(_session);
        Navigator.ToEditorUnit(_session, document, isNew: true);
    }

    private void OpenNewBuilding()
    {
        if (_session is null || _session.Type != ContentModuleType.Buildings)
            return;

        var document = Hub.CreateBuilding(_session);
        Navigator.ToEditorBuilding(_session, document, isNew: true);
    }

    private void OpenExistingMap(string mapId)
    {
        if (_session is null)
            return;

        try
        {
            var document = Hub.LoadMap(_session, mapId);
            Navigator.ToEditorMapPaint(_session, document, isNewMap: false);
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
            Hub.DeleteMap(_session, mapId);
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
            var document = Hub.LoadLevel(_session, levelId);
            Navigator.ToEditorLevel(_session, document, isNew: false);
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
            Hub.DeleteLevel(_session, levelId);
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
            var document = Hub.LoadUnit(_session, unitId);
            Navigator.ToEditorUnit(_session, document, isNew: false);
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
            Hub.DeleteUnit(_session, unitId);
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
            var document = Hub.LoadBuilding(_session, buildingId);
            Navigator.ToEditorBuilding(_session, document, isNew: false);
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
            Hub.DeleteBuilding(_session, buildingId);
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
            var openedCopy = row.Source != ContentModuleSource.UserLibrary;
            _session = Hub.OpenListedModule(row);
            RefreshLibrary(
                openedCopy
                    ? $"Duplicated '{row.ModuleId}' → '{_session.ModuleId}' and opened."
                    : $"Opened '{row.ModuleId}'.");
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

        Navigator.ToMainMenu();
    }
}
