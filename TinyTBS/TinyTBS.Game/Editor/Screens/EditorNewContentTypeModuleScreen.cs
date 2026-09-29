using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Presentation.Menu;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Id/Title wizard before creating a user units or buildings module.</summary>
public sealed class EditorNewContentTypeModuleScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorWorkspaceSession? _session;
    private readonly ContentModuleType _moduleType;
    private readonly EditorNewContentTypeModuleView _view = new();
    private MainMenuBackground? _background;
    private ContentTypeModuleWriter? _moduleWriter;

    private bool _pendingGoToHub;
    private bool _pendingCreate;

    public EditorNewContentTypeModuleScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession? session,
        ContentModuleType moduleType)
        : base(game)
    {
        _assets = assets;
        _session = session;
        if (moduleType is not (ContentModuleType.Units or ContentModuleType.Buildings or ContentModuleType.Theme))
            throw new ArgumentOutOfRangeException(nameof(moduleType), "Unsupported module type for this wizard.");
        _moduleType = moduleType;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();
        TinyGame.UserDataPaths.EnsureCreated();
        _moduleWriter = new ContentTypeModuleWriter(TinyGame.Files, TinyGame.UserDataPaths);
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);

        var baseId = _moduleType switch
        {
            ContentModuleType.Units => "user_units",
            ContentModuleType.Buildings => "user_buildings",
            ContentModuleType.Theme => "user_theme",
            _ => "user_module",
        };
        var defaultId = _moduleWriter.AllocateUniqueModuleId(baseId);
        var defaultTitle = _moduleType switch
        {
            ContentModuleType.Units => defaultId == "user_units" ? "User Units" : "User Units (" + defaultId + ")",
            ContentModuleType.Buildings => defaultId == "user_buildings"
                ? "User Buildings"
                : "User Buildings (" + defaultId + ")",
            ContentModuleType.Theme => defaultId == "user_theme" ? "User Theme" : "User Theme (" + defaultId + ")",
            _ => defaultId,
        };

        _view.Build(_moduleType, defaultId, defaultTitle, RequestCreate, RequestGoToHub);
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        _moduleWriter = null;
        _pendingGoToHub = false;
        _pendingCreate = false;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        _view.HandleInput(
            TinyGame.Commands,
            (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (_view.IsTextEntryActive)
            return;

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
        {
            RequestGoToHub();
        }

        if (_pendingCreate)
        {
            _pendingCreate = false;
            CreateModule();
            return;
        }

        if (_pendingGoToHub)
        {
            _pendingGoToHub = false;
            GoToHub();
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

    private void RequestGoToHub() => _pendingGoToHub = true;

    private void RequestCreate() => _pendingCreate = true;

    private void CreateModule()
    {
        ArgumentNullException.ThrowIfNull(_moduleWriter);

        try
        {
            var baseId = _moduleType switch
            {
                ContentModuleType.Units => "user_units",
                ContentModuleType.Buildings => "user_buildings",
                ContentModuleType.Theme => "user_theme",
                _ => "user_module",
            };
            var moduleId = SanitizeModuleId(_view.ModuleId, baseId);
            var title = string.IsNullOrWhiteSpace(_view.ModuleTitle) ? moduleId : _view.ModuleTitle.Trim();
            if (Directory.Exists(TinyGame.Files.Combine(TinyGame.UserDataPaths.Modules, moduleId)))
                moduleId = _moduleWriter.AllocateUniqueModuleId(moduleId);

            var root = _moduleWriter.CreateNew(
                _moduleType,
                moduleId,
                title,
                description: "Created in TinyTBS Editor.");

            var session = new EditorWorkspaceSession(
                moduleId,
                root,
                _moduleType,
                title);
            ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, session));
        }
        catch (Exception exception) when (exception is EditorException or TinymodInstallException or IOException)
        {
            _view.SyncStatus("Create failed: " + exception.Message);
        }
    }

    private static string SanitizeModuleId(string raw, string fallback)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim();
        try
        {
            ContentModuleManifestParser.ValidateModuleId(trimmed);
            return trimmed;
        }
        catch (TinymodInstallException)
        {
            return fallback;
        }
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, _session));
}
