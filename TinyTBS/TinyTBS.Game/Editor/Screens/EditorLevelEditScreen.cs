using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Levels;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Presentation.Menu;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Create or edit a level.json under the open scenario.</summary>
public sealed class EditorLevelEditScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorWorkspaceSession _session;
    private readonly EditableLevelDocument _document;
    private readonly bool _isNew;
    private readonly EditorLevelEditView _view = new();
    private MainMenuBackground? _background;
    private LevelDocumentWriter? _writer;

    public EditorLevelEditScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession session,
        EditableLevelDocument document,
        bool isNew)
        : base(game)
    {
        _assets = assets;
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isNew = isNew;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();
        _writer = new LevelDocumentWriter(TinyGame.Files);
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);
        var maps = ListMapIds(_session.ModuleRootPath);
        if (maps.Count > 0 && (_isNew || string.IsNullOrWhiteSpace(_document.MapIdFromRef())))
            _document.SetMapId(maps[0]);
        _view.Build(_document, maps, Save, GoToHub);
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        _writer = null;
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
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
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

    private void Save()
    {
        if (_writer is null)
            return;

        try
        {
            _view.ApplyTextFields();
            var id = SanitizeId(_document.Id);
            _document.Id = id;
            if (_isNew)
            {
                var existing = TinyGame.Files.Combine(_session.ModuleRootPath, "Levels", id);
                if (Directory.Exists(existing))
                    id = AllocateLevelId(id);
                _document.Id = id;
            }

            ContentModuleManifestParser.ValidateModuleId(_document.MapIdFromRef());
            _writer.Write(_session.ModuleRootPath, _document);
            _view.SyncStatus("Saved Levels/" + _document.Id);
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private static string SanitizeId(string raw)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? "level" : raw.Trim();
        try
        {
            ContentModuleManifestParser.ValidateModuleId(trimmed);
            return trimmed;
        }
        catch (TinymodInstallException)
        {
            return "level";
        }
    }

    private string AllocateLevelId(string stem)
    {
        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = stem + "_" + suffix;
            if (!Directory.Exists(TinyGame.Files.Combine(_session.ModuleRootPath, "Levels", candidate)))
                return candidate;
        }

        throw new EditorException("Could not allocate a unique level id.");
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

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, _session));
}
