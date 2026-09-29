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

/// <summary>Id/Title wizard before creating a user scenario module.</summary>
public sealed class EditorNewScenarioScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorWorkspaceSession? _session;
    private readonly EditorNewScenarioView _view = new();
    private MainMenuBackground? _background;
    private ScenarioModuleWriter? _scenarioWriter;

    public EditorNewScenarioScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession? session)
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
        _scenarioWriter = new ScenarioModuleWriter(TinyGame.Files, TinyGame.UserDataPaths);
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);

        var defaultId = _scenarioWriter.AllocateUniqueModuleId();
        var defaultTitle = defaultId == "user_scenario"
            ? "User Scenario"
            : "User Scenario (" + defaultId + ")";
        _view.Build(defaultId, defaultTitle, CreateScenario, GoToHub);
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        _scenarioWriter = null;
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
            || TinyGame.Commands.WasPressed(GameCommand.Cancel))
        {
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

    private void CreateScenario()
    {
        ArgumentNullException.ThrowIfNull(_scenarioWriter);

        try
        {
            var moduleId = SanitizeModuleId(_view.ModuleId);
            var title = string.IsNullOrWhiteSpace(_view.ModuleTitle) ? moduleId : _view.ModuleTitle.Trim();
            if (Directory.Exists(TinyGame.Files.Combine(TinyGame.UserDataPaths.Modules, moduleId)))
                moduleId = _scenarioWriter.AllocateUniqueModuleId(moduleId);

            var root = _scenarioWriter.CreateNew(
                moduleId,
                title,
                description: "Created in TinyTBS Editor.");

            var session = new EditorWorkspaceSession(
                moduleId,
                root,
                ContentModuleType.Scenario,
                title);
            ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, session));
        }
        catch (Exception exception) when (exception is EditorException or TinymodInstallException or IOException)
        {
            _view.SyncStatus("Create failed: " + exception.Message);
        }
    }

    private static string SanitizeModuleId(string raw)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? "user_scenario" : raw.Trim();
        try
        {
            ContentModuleManifestParser.ValidateModuleId(trimmed);
            return trimmed;
        }
        catch (TinymodInstallException)
        {
            return "user_scenario";
        }
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, _session));
}
