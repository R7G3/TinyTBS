using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Core.Assets;
using TinyTBS.Core.Match;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation;
using TinyTBS.Game.Rendering;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Thin frame glue: wires match logic, HUD presentation, and engine scene/draw helpers.
/// </summary>
public sealed class GameplayScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly GameplayHudViewModel _hud = new();
    private readonly GameplayHudView _hudView = new();

    private MatchState? _match;
    private MatchScene? _scene;
    private CursorHighlightRenderer? _cursorHighlight;
    private LoadedTexture? _unitAsset;

    public GameplayScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        _unitAsset = GameTextureLoader.LoadOrFallback(
            GraphicsDevice,
            Content,
            _assets,
            logicalRelativePath: "Images/placeholder.png",
            contentAssetName: "Images/placeholder");

        _match = MatchState.CreateDemo();
        _scene = new MatchScene(
            _match,
            GraphicsDevice,
            TinyGame.SharedSpriteBatch,
            _unitAsset.Value.Texture);
        _cursorHighlight = new CursorHighlightRenderer(GraphicsDevice);

        _hudView.Build(_hud, onEndTurn: () => _match?.EndTurn(), onMenu: ReturnToMenu);
        SyncHud();
    }

    public override void UnloadContent()
    {
        _hudView.Clear();

        _scene?.Dispose();
        _scene = null;
        _match = null;

        _cursorHighlight?.Dispose();
        _cursorHighlight = null;

        _unitAsset?.DisposeIfOwned();
        _unitAsset = null;

        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        if (_match is null || _scene is null)
            return;

        _scene.PrepareFrame(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

        if (MatchCommandApplicator.Apply(_match, TinyGame.Commands))
        {
            ReturnToMenu();
            return;
        }

        if (_match is null || _scene is null)
            return;

        MatchCommandApplicator.ApplyPointer(_match, TinyGame.Pointer, _scene.Layout);

        if (_match is null || _scene is null)
            return;

        _scene.Update(gameTime);
        SyncHud();
        GumService.Default.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 20, 28));

        if (_match is not null && _scene is not null)
        {
            _scene.PrepareFrame(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _scene.Draw(gameTime);
            _cursorHighlight?.Draw(
                TinyGame.SharedSpriteBatch,
                _scene.Layout,
                _match.Cursor,
                hasSelection: _match.SelectedUnitId is not null);
        }

        GumService.Default.Draw();
    }

    private void SyncHud()
    {
        if (_match is null)
            return;

        _hud.StatusText = _match.StatusText;
        _hudView.Sync(_hud);
    }

    private void ReturnToMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, _assets));
}
