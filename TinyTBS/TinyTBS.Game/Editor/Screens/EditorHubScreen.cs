using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
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
/// Editor hub: modules, CoW duplicate, open session, New Map / open existing map (slice 1–2).
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

        RefreshLibrary(
            _session is null
                ? "Create a scenario or Confirm a bundled module to copy into your library."
                : $"Editing '{_session.ModuleId}'. New Map or open a map below.");
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

        // If map detail was open, HandleInput may close it on B/Esc — do not also leave the hub.
        var detailWasOpen = _view.IsMapDetailOpen;
        _view.HandleInput(TinyGame.Commands, (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
        {
            if (detailWasOpen || _view.IsMapDetailOpen)
            {
                _view.TryCloseMapDetail();
                return;
            }

            GoToMainMenu();
        }
    }

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
            onActivateModule: ActivateModule,
            onOpenMap: OpenExistingMap,
            onDeleteMap: DeleteMap,
            onCloseModule: CloseModule,
            onBack: GoToMainMenu);
    }

    private void SyncOpenFields()
    {
        if (_session is null)
        {
            _viewModel.OpenModuleId = null;
            _viewModel.OpenModuleTitle = null;
            _viewModel.CanCreateMap = false;
            _viewModel.Maps = [];
            return;
        }

        _viewModel.OpenModuleId = _session.ModuleId;
        _viewModel.OpenModuleTitle = _session.Title;
        _viewModel.CanCreateMap = _session.Type == ContentModuleType.Scenario;
        _viewModel.Maps = ListMapIds(_session.ModuleRootPath);
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

    private void OpenNewScenarioWizard() =>
        ScreenManager.ReplaceScreen(new EditorNewScenarioScreen(TinyGame, _assets, _session));

    private void OpenNewMapWizard()
    {
        if (_session is null || _session.Type != ContentModuleType.Scenario)
            return;

        ScreenManager.ReplaceScreen(new EditorNewMapScreen(TinyGame, _assets, _session));
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

            var levelRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Levels", mapId);
            if (Directory.Exists(levelRoot))
                Directory.Delete(levelRoot, recursive: true);

            RefreshLibrary($"Deleted map '{mapId}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _viewModel.StatusText = "Delete map failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
        }
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

    private void GoToMainMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, _assets));
}
