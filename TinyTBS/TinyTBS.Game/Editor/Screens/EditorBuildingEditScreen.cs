using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Presentation.Menu;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Create or edit Buildings/{id}.json in an open buildings module.</summary>
public sealed class EditorBuildingEditScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorWorkspaceSession _session;
    private readonly EditableBuildingDocument _document;
    private readonly bool _isNew;
    private readonly EditorBuildingEditView _view = new();
    private MainMenuBackground? _background;
    private BuildingDocumentWriter? _writer;

    public EditorBuildingEditScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession session,
        EditableBuildingDocument document,
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
        _writer = new BuildingDocumentWriter(TinyGame.Files);
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);
        _view.Build(_document, Save, GoToHub);
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

        var overlayWasOpen = _view.IsOverlayOpen;
        _view.HandleInput(
            TinyGame.Commands,
            TinyGame.Pointer,
            (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (overlayWasOpen || _view.IsOverlayOpen)
            return;

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
            ContentModuleManifestParser.ValidateModuleId(id);

            var buildingsDir = TinyGame.Files.Combine(_session.ModuleRootPath, "Buildings");
            var targetPath = TinyGame.Files.Combine(buildingsDir, id + ".json");
            var renamed = !_isNew
                && !string.Equals(id, _document.OriginalId, StringComparison.Ordinal);
            if ((_isNew || renamed) && File.Exists(targetPath))
            {
                id = AllocateUniqueBuildingId(buildingsDir, id);
                _document.Id = id;
            }

            _writer.Write(_session.ModuleRootPath, _document);
            _view.SyncIdentityFromDocument();
            _view.SyncStatus("Saved Buildings/" + _document.Id + ".json");
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private static string SanitizeId(string raw)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? "building" : raw.Trim();
        try
        {
            ContentModuleManifestParser.ValidateModuleId(trimmed);
            return trimmed;
        }
        catch (TinymodInstallException)
        {
            return "building";
        }
    }

    private string AllocateUniqueBuildingId(string buildingsDir, string stem)
    {
        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = stem + "_" + suffix;
            var path = TinyGame.Files.Combine(buildingsDir, candidate + ".json");
            if (!File.Exists(path))
                return candidate;
        }

        throw new EditorException("Could not allocate a unique building id.");
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, _session));
}
