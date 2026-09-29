using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Bundles;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Presentation.Menu;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Create or edit a user <c>*.bundle.json</c> preset.</summary>
public sealed class EditorBundleEditScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorWorkspaceSession? _session;
    private readonly EditableBundleDocument _document;
    private readonly bool _isNew;
    private readonly EditorBundleEditView _view = new();
    private MainMenuBackground? _background;
    private BundleDocumentWriter? _writer;
    private ContentModuleLibrary? _moduleLibrary;

    public EditorBundleEditScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession? session,
        EditableBundleDocument document,
        bool isNew)
        : base(game)
    {
        _assets = assets;
        _session = session;
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isNew = isNew;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();
        TinyGame.UserDataPaths.EnsureCreated();
        _writer = new BundleDocumentWriter(TinyGame.Files, TinyGame.UserDataPaths);
        _moduleLibrary = new ContentModuleLibrary(TinyGame.Files, TinyGame.UserDataPaths);
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);

        var modules = _moduleLibrary.ListEffectiveModules();
        _view.Build(_document, modules, _isNew, Save, GoToHub);
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        _writer = null;
        _moduleLibrary = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);

        var overlayWasOpen = _view.IsOverlayOpen;
        _view.HandleInput(TinyGame.Commands, (float)gameTime.ElapsedGameTime.TotalSeconds);

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

            if ((_isNew || !string.Equals(id, _document.OriginalId, StringComparison.Ordinal))
                && File.Exists(TinyGame.Files.Combine(
                    TinyGame.UserDataPaths.Bundles,
                    id + ContentBundleFiles.BundleJsonExtension)))
            {
                id = _writer.AllocateUniqueBundleId(id);
                _document.Id = id;
            }

            _writer.Write(_document);
            _view.SyncIdentityFromDocument();
            _view.SyncStatus("Saved Bundles/" + _document.Id + ContentBundleFiles.BundleJsonExtension);
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private static string SanitizeId(string raw)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? "user_bundle" : raw.Trim();
        if (trimmed.Contains("..", StringComparison.Ordinal)
            || trimmed.Contains('/', StringComparison.Ordinal)
            || trimmed.Contains('\\', StringComparison.Ordinal)
            || trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return "user_bundle";
        }

        return trimmed;
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, _session));
}
