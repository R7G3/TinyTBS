using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Editor.Bundles;
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
using TinyTBS.Game.Presentation.Menu;
using TinyTBS.Game.Screens;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>
/// Editor hub: modules, CoW duplicate, open session, maps/levels/campaign/units/buildings.
/// </summary>
public sealed class EditorHubScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorHubViewModel _viewModel = new();
    private readonly EditorHubView _view = new();

    private MainMenuBackground? _background;
    private ContentModuleLibrary? _moduleLibrary;
    private ContentBundleLibrary? _bundleLibrary;
    private ScenarioModuleWriter? _scenarioWriter;
    private BundleDocumentWriter? _bundleWriter;
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
        : base(game)
    {
        _assets = assets;
        _session = session;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        TinyGame.UserDataPaths.EnsureCreated();
        _moduleLibrary = new ContentModuleLibrary(TinyGame.Files, TinyGame.UserDataPaths);
        _bundleLibrary = new ContentBundleLibrary(TinyGame.Files, TinyGame.UserDataPaths);
        _scenarioWriter = new ScenarioModuleWriter(TinyGame.Files, TinyGame.UserDataPaths);
        _bundleWriter = new BundleDocumentWriter(TinyGame.Files, TinyGame.UserDataPaths);
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);

        BeginInputSuppress();
        RefreshLibrary(
            _session is null
                ? "Create a scenario or Confirm a bundled module to copy into your library."
                : $"Editing '{_session.ModuleId}'. Content below.");
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        _moduleLibrary = null;
        _bundleLibrary = null;
        _scenarioWriter = null;
        _bundleWriter = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        _view.ApplyResponsiveLayout();

        if (_suppressInputUntilIdle)
        {
            if (IsIngressBusy(TinyGame.Commands, TinyGame.Pointer))
                return;

            EndInputSuppress();
        }

        var detailWasOpen = _view.IsAnyDetailOpen;
        _view.HandleInput(TinyGame.Commands, (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
        {
            if (detailWasOpen || _view.IsAnyDetailOpen)
            {
                _view.TryCloseAnyDetail();
                return;
            }

            HandleBack();
        }
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

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 28, 38));

        _background?.Draw(
            TinyGame.SharedSpriteBatch,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height,
            gameTime);

        GumService.Default.Draw();
    }

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
        _viewModel.Maps = _session.Type == ContentModuleType.Scenario
            ? ListMapIds(_session.ModuleRootPath)
            : [];
        _viewModel.Levels = _session.Type == ContentModuleType.Scenario
            ? ListLevelIds(_session.ModuleRootPath)
            : [];
        _viewModel.Units = _session.Type == ContentModuleType.Units
            ? ListJsonIds(_session.ModuleRootPath, "Units")
            : [];
        _viewModel.Buildings = _session.Type == ContentModuleType.Buildings
            ? ListJsonIds(_session.ModuleRootPath, "Buildings")
            : [];
    }

    private static IReadOnlyList<string> ListMapIds(string moduleRoot)
    {
        var mapsRoot = Path.Combine(moduleRoot, "Maps");
        if (!Directory.Exists(mapsRoot))
            return [];

        return Directory.GetDirectories(mapsRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> ListLevelIds(string moduleRoot)
    {
        var levelsRoot = Path.Combine(moduleRoot, "Levels");
        if (!Directory.Exists(levelsRoot))
            return [];

        return Directory.GetDirectories(levelsRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> ListJsonIds(string moduleRoot, string folderName)
    {
        var folder = Path.Combine(moduleRoot, folderName);
        if (!Directory.Exists(folder))
            return [];

        return Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void OpenNewScenarioWizard() =>
        ScreenManager.ReplaceScreen(new EditorNewScenarioScreen(TinyGame, _assets, _session));

    private void OpenNewContentTypeWizard(ContentModuleType type) =>
        ScreenManager.ReplaceScreen(new EditorNewContentTypeModuleScreen(TinyGame, _assets, _session, type));

    private void OpenThemeEditor()
    {
        if (_session is null || _session.Type != ContentModuleType.Theme)
            return;

        ScreenManager.ReplaceScreen(new EditorThemeEditScreen(TinyGame, _assets, _session));
    }

    private void OpenNewBundle()
    {
        ArgumentNullException.ThrowIfNull(_moduleLibrary);
        ArgumentNullException.ThrowIfNull(_bundleWriter);

        var modules = _moduleLibrary.ListEffectiveModules();
        var bundleId = _bundleWriter.AllocateUniqueBundleId("user_bundle");
        var document = EditableBundleDocument.CreateDefault(bundleId, modules);
        ScreenManager.ReplaceScreen(
            new EditorBundleEditScreen(TinyGame, _assets, _session, document, isNew: true));
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
                new EditorBundleEditScreen(TinyGame, _assets, _session, document, isNew: false));
        }
        catch (Exception exception)
        {
            _viewModel.StatusText = "Open bundle failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
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
            {
                _viewModel.StatusText = $"Bundle '{bundleId}' was already gone.";
                _view.SyncStatus(_viewModel);
            }
        }
        catch (Exception exception) when (exception is EditorException or IOException or UnauthorizedAccessException)
        {
            _viewModel.StatusText = "Delete bundle failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void ActivateBundle(EditorBundleRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(_bundleWriter);
        ArgumentNullException.ThrowIfNull(_bundleLibrary);

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
                new EditorBundleEditScreen(TinyGame, _assets, _session, document, isNew: false));
        }
        catch (Exception exception) when (exception is EditorException or ContentBundleException or IOException)
        {
            _viewModel.StatusText = "Open/Duplicate bundle failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
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
            _viewModel.StatusText = "Export failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void OpenNewMapWizard()
    {
        if (_session is null || _session.Type != ContentModuleType.Scenario)
            return;

        ScreenManager.ReplaceScreen(new EditorNewMapScreen(TinyGame, _assets, _session));
    }

    private void OpenNewLevelWizard()
    {
        if (_session is null || _session.Type != ContentModuleType.Scenario)
            return;

        var maps = ListMapIds(_session.ModuleRootPath);
        var mapId = maps.Count > 0 ? maps[0] : "map";
        var levelId = AllocateUniqueChildId(_session.ModuleRootPath, "Levels", mapId);
        var document = TinyTBS.Game.Editor.Levels.EditableLevelDocument.CreateDefault(
            levelId,
            title: levelId,
            mapId: mapId);
        ScreenManager.ReplaceScreen(
            new EditorLevelEditScreen(TinyGame, _assets, _session, document, isNew: true));
    }

    private void OpenCampaignEditor()
    {
        if (_session is null || _session.Type != ContentModuleType.Scenario)
            return;

        ScreenManager.ReplaceScreen(new EditorCampaignEditScreen(TinyGame, _assets, _session));
    }

    private void OpenNewUnit()
    {
        if (_session is null || _session.Type != ContentModuleType.Units)
            return;

        var unitId = AllocateUniqueJsonId(_session.ModuleRootPath, "Units", "unit");
        var document = EditableUnitDocument.CreateDefault(unitId);
        ScreenManager.ReplaceScreen(
            new EditorUnitEditScreen(TinyGame, _assets, _session, document, isNew: true));
    }

    private void OpenNewBuilding()
    {
        if (_session is null || _session.Type != ContentModuleType.Buildings)
            return;

        var buildingId = AllocateUniqueJsonId(_session.ModuleRootPath, "Buildings", "building");
        var document = EditableBuildingDocument.CreateDefault(buildingId);
        ScreenManager.ReplaceScreen(
            new EditorBuildingEditScreen(TinyGame, _assets, _session, document, isNew: true));
    }

    private void OpenExistingMap(string mapId)
    {
        if (_session is null)
            return;

        try
        {
            var mapRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Maps", mapId);
            var definition = MapFolderLoader.Load(mapRoot, TinyGame.Files);
            var document = EditableMapDocument.FromDefinition(definition);
            ScreenManager.ReplaceScreen(
                new EditorMapPaintScreen(TinyGame, _assets, _session, document, isNewMap: false));
        }
        catch (Exception exception)
        {
            _viewModel.StatusText = "Open map failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void DeleteMap(string mapId)
    {
        if (_session is null)
            return;

        try
        {
            var mapRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Maps", mapId);
            if (Directory.Exists(mapRoot))
                Directory.Delete(mapRoot, recursive: true);

            RefreshLibrary($"Deleted map '{mapId}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _viewModel.StatusText = "Delete map failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void OpenExistingLevel(string levelId)
    {
        if (_session is null)
            return;

        try
        {
            var levelRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Levels", levelId);
            var document = TinyTBS.Game.Editor.Levels.EditableLevelDocument.Load(levelRoot, TinyGame.Files);
            ScreenManager.ReplaceScreen(
                new EditorLevelEditScreen(TinyGame, _assets, _session, document, isNew: false));
        }
        catch (Exception exception)
        {
            _viewModel.StatusText = "Open level failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void DeleteLevel(string levelId)
    {
        if (_session is null)
            return;

        try
        {
            var levelRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Levels", levelId);
            if (Directory.Exists(levelRoot))
                Directory.Delete(levelRoot, recursive: true);

            RefreshLibrary($"Deleted level '{levelId}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _viewModel.StatusText = "Delete level failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void OpenExistingUnit(string unitId)
    {
        if (_session is null)
            return;

        try
        {
            var path = TinyGame.Files.Combine(_session.ModuleRootPath, "Units", unitId + ".json");
            var document = EditableUnitDocument.Load(path, TinyGame.Files);
            ScreenManager.ReplaceScreen(
                new EditorUnitEditScreen(TinyGame, _assets, _session, document, isNew: false));
        }
        catch (Exception exception)
        {
            _viewModel.StatusText = "Open unit failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void DeleteUnit(string unitId)
    {
        if (_session is null)
            return;

        try
        {
            var path = TinyGame.Files.Combine(_session.ModuleRootPath, "Units", unitId + ".json");
            if (File.Exists(path))
                File.Delete(path);

            RemoveFromRecruitPool(_session.ModuleRootPath, unitId);
            RefreshLibrary($"Deleted unit '{unitId}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or EditorException)
        {
            _viewModel.StatusText = "Delete unit failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void OpenExistingBuilding(string buildingId)
    {
        if (_session is null)
            return;

        try
        {
            var path = TinyGame.Files.Combine(_session.ModuleRootPath, "Buildings", buildingId + ".json");
            var document = EditableBuildingDocument.Load(path, TinyGame.Files);
            ScreenManager.ReplaceScreen(
                new EditorBuildingEditScreen(TinyGame, _assets, _session, document, isNew: false));
        }
        catch (Exception exception)
        {
            _viewModel.StatusText = "Open building failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void DeleteBuilding(string buildingId)
    {
        if (_session is null)
            return;

        try
        {
            var path = TinyGame.Files.Combine(_session.ModuleRootPath, "Buildings", buildingId + ".json");
            if (File.Exists(path))
                File.Delete(path);

            RefreshLibrary($"Deleted building '{buildingId}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _viewModel.StatusText = "Delete building failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private void RemoveFromRecruitPool(string moduleRoot, string localUnitId)
    {
        var moduleJsonPath = TinyGame.Files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!File.Exists(moduleJsonPath))
            return;

        System.Text.Json.Nodes.JsonNode? root;
        try
        {
            root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(moduleJsonPath));
        }
        catch (System.Text.Json.JsonException)
        {
            return;
        }

        if (root is not System.Text.Json.Nodes.JsonObject rootObject)
            return;
        if (rootObject["recruit"] is not System.Text.Json.Nodes.JsonObject recruitObject)
            return;
        if (recruitObject["addsToPool"] is not System.Text.Json.Nodes.JsonArray existing)
            return;

        var contentNamespace = rootObject["namespace"]?.GetValue<string>()?.Trim();
        if (string.IsNullOrWhiteSpace(contentNamespace))
            contentNamespace = rootObject["id"]?.GetValue<string>()?.Trim() ?? string.Empty;

        var fullId = string.IsNullOrWhiteSpace(contentNamespace)
            ? localUnitId
            : contentNamespace + "/" + localUnitId;

        var pool = new System.Text.Json.Nodes.JsonArray();
        foreach (var entry in existing)
        {
            var value = entry?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(value))
                continue;
            if (string.Equals(value, fullId, StringComparison.Ordinal)
                || string.Equals(value, localUnitId, StringComparison.Ordinal))
            {
                continue;
            }

            pool.Add(value.Trim());
        }

        recruitObject["addsToPool"] = pool;
        File.WriteAllText(
            moduleJsonPath,
            rootObject.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
                + Environment.NewLine);
    }

    private static string AllocateUniqueChildId(string moduleRoot, string folderName, string stem)
    {
        var root = Path.Combine(moduleRoot, folderName);
        Directory.CreateDirectory(root);
        if (!Directory.Exists(Path.Combine(root, stem)))
            return stem;

        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = stem + "_" + suffix;
            if (!Directory.Exists(Path.Combine(root, candidate)))
                return candidate;
        }

        throw new EditorException("Could not allocate a unique id under " + folderName + ".");
    }

    private static string AllocateUniqueJsonId(string moduleRoot, string folderName, string stem)
    {
        var root = Path.Combine(moduleRoot, folderName);
        Directory.CreateDirectory(root);
        if (!File.Exists(Path.Combine(root, stem + ".json")))
            return stem;

        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = stem + "_" + suffix;
            if (!File.Exists(Path.Combine(root, candidate + ".json")))
                return candidate;
        }

        throw new EditorException("Could not allocate a unique json id under " + folderName + ".");
    }

    private void ActivateModule(EditorModuleRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(_scenarioWriter);

        try
        {
            if (row.Source == ContentModuleSource.UserLibrary)
            {
                OpenSession(row.ModuleId, row.ModuleRootPath, row.Type, row.Title);
                RefreshLibrary($"Opened '{row.ModuleId}'.");
                return;
            }

            var targetId = AllocateCopyId(row.ModuleId);
            var destination = ModuleDirectoryCopier.CopyToUserLibrary(
                row.ModuleRootPath,
                targetId,
                TinyGame.Files,
                TinyGame.UserDataPaths);

            if (!string.Equals(targetId, row.ModuleId, StringComparison.Ordinal))
            {
                ModuleManifestIdRewriter.RewriteIdentity(
                    destination,
                    targetId,
                    TinyGame.Files,
                    title: row.Title + " (copy)");
            }

            var title = targetId == row.ModuleId ? row.Title : row.Title + " (copy)";
            OpenSession(targetId, destination, row.Type, title);
            RefreshLibrary($"Duplicated '{row.ModuleId}' → '{targetId}' and opened.");
        }
        catch (Exception exception) when (exception is EditorException or TinymodInstallException or IOException)
        {
            _viewModel.StatusText = "Open/Duplicate failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
    }

    private string AllocateCopyId(string sourceModuleId)
    {
        ArgumentNullException.ThrowIfNull(_scenarioWriter);

        ContentModuleManifestParser.ValidateModuleId(sourceModuleId);
        var userRoot = TinyGame.Files.Combine(TinyGame.UserDataPaths.Modules, sourceModuleId);
        if (!Directory.Exists(userRoot))
            return sourceModuleId;

        return _scenarioWriter.AllocateUniqueModuleId(sourceModuleId + "_copy");
    }

    private void OpenSession(
        string moduleId,
        string moduleRootPath,
        ContentModuleType type,
        string title)
    {
        _session = new EditorWorkspaceSession(moduleId, moduleRootPath, type, title);
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
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, _assets));
}
