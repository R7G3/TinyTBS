using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Input;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Editor.Presentation;
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
/// Editor hub: modules, CoW duplicate, open session, maps/levels/campaign (slice 1–4).
/// </summary>
public sealed class EditorHubScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorHubViewModel _viewModel = new();
    private readonly EditorHubView _view = new();

    private MainMenuBackground? _background;
    private ContentModuleLibrary? _moduleLibrary;
    private ScenarioModuleWriter? _scenarioWriter;
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
        _scenarioWriter = new ScenarioModuleWriter(TinyGame.Files, TinyGame.UserDataPaths);
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);

        BeginInputSuppress();
        RefreshLibrary(
            _session is null
                ? "Create a scenario or Confirm a bundled module to copy into your library."
                : $"Editing '{_session.ModuleId}'. Maps, levels, or campaign below.");
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        _moduleLibrary = null;
        _scenarioWriter = null;
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

        // If detail was open, HandleInput may close it on B/Esc — do not also leave the hub.
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

        var texture = _background?.Texture;
        if (texture is not null)
        {
            ViewportFit.DrawCentered(
                TinyGame.SharedSpriteBatch,
                texture,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                Color.White * 0.35f);
        }

        GumService.Default.Draw();
    }

    private void RefreshLibrary(string statusText)
    {
        ArgumentNullException.ThrowIfNull(_moduleLibrary);

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

        _viewModel.Modules = modules;
        _viewModel.StatusText = statusText;
        _viewModel.CanPublish = false;
        SyncOpenFields();
        _view.Build(
            _viewModel,
            onNewScenario: OpenNewScenarioWizard,
            onNewMap: OpenNewMapWizard,
            onNewLevel: OpenNewLevelWizard,
            onEditCampaign: OpenCampaignEditor,
            onActivateModule: ActivateModule,
            onOpenMap: OpenExistingMap,
            onDeleteMap: DeleteMap,
            onOpenLevel: OpenExistingLevel,
            onDeleteLevel: DeleteLevel,
            onCloseModule: CloseModule,
            onBack: HandleBack);
    }

    private void SyncOpenFields()
    {
        if (_session is null)
        {
            _viewModel.OpenModuleId = null;
            _viewModel.OpenModuleTitle = null;
            _viewModel.CanCreateMap = false;
            _viewModel.Maps = [];
            _viewModel.Levels = [];
            return;
        }

        _viewModel.OpenModuleId = _session.ModuleId;
        _viewModel.OpenModuleTitle = _session.Title;
        _viewModel.CanCreateMap = _session.Type == ContentModuleType.Scenario;
        _viewModel.Maps = ListMapIds(_session.ModuleRootPath);
        _viewModel.Levels = ListLevelIds(_session.ModuleRootPath);
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

    private void OpenNewScenarioWizard() =>
        ScreenManager.ReplaceScreen(new EditorNewScenarioScreen(TinyGame, _assets, _session));

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
