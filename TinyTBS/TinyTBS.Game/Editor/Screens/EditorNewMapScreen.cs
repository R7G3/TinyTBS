using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Presentation.Menu;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Size picker before opening the map paint screen.</summary>
public sealed class EditorNewMapScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorWorkspaceSession _session;
    private readonly EditorNewMapView _view = new();
    private MainMenuBackground? _background;

    public EditorNewMapScreen(GameMain game, IAssetResolver assets, EditorWorkspaceSession session)
        : base(game)
    {
        _assets = assets;
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);
        var defaultMapId = AllocateMapId();
        _view.Build(_session.ModuleId, defaultMapId, StartPaint, GoToHub);
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        _view.HandleInput(
            TinyGame.Commands,
            TinyGame.Pointer,
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

    private void StartPaint()
    {
        var mapId = SanitizeMapId(_view.MapId);
        var mapTitle = string.IsNullOrWhiteSpace(_view.MapTitle) ? mapId : _view.MapTitle.Trim();
        var mapsRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Maps");
        Directory.CreateDirectory(mapsRoot);
        if (Directory.Exists(TinyGame.Files.Combine(mapsRoot, mapId)))
            mapId = AllocateMapId(mapId);

        var document = EditableMapDocument.CreateFilled(
            mapId,
            title: mapTitle,
            _view.Width,
            _view.Height,
            terrainType: "grass");
        document.IsDirty = true;

        ScreenManager.ReplaceScreen(
            new EditorMapPaintScreen(TinyGame, _assets, _session, document, isNewMap: true));
    }

    private static string SanitizeMapId(string raw)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? "map" : raw.Trim();
        try
        {
            ContentModuleManifestParser.ValidateModuleId(trimmed);
            return trimmed;
        }
        catch (TinymodInstallException)
        {
            return "map";
        }
    }

    private string AllocateMapId(string stem = "map")
    {
        var mapsRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Maps");
        Directory.CreateDirectory(mapsRoot);
        if (!Directory.Exists(TinyGame.Files.Combine(mapsRoot, stem)))
            return stem;

        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = stem + "_" + suffix;
            if (!Directory.Exists(TinyGame.Files.Combine(mapsRoot, candidate)))
                return candidate;
        }

        throw new EditorException("Could not allocate a unique map id.");
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, _session));
}
