using Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Screens;
using TinyTBS.Core.Assets;
using TinyTBS.Core.Input;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation;
using TinyTBS.Game.Rendering;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Thin frame glue: wires match logic, HUD presentation, and engine draw helpers.
/// </summary>
public sealed class GameplayScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly GameplayHudViewModel _hud = new();
    private readonly GameplayHudView _hudView = new();

    private MatchSession? _match;
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
        _match = new MatchSession(GraphicsDevice, TinyGame.SharedSpriteBatch, _unitAsset.Value.Texture);
        _cursorHighlight = new CursorHighlightRenderer(GraphicsDevice);

        _hudView.Build(_hud, onEndTurn: () => _match?.EndTurn(), onMenu: ReturnToMenu);
        SyncHud();
    }

    public override void UnloadContent()
    {
        _hudView.Clear();

        _match?.Dispose();
        _match = null;

        _cursorHighlight?.Dispose();
        _cursorHighlight = null;

        _unitAsset?.DisposeIfOwned();
        _unitAsset = null;

        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        if (_match is null)
            return;

        _match.PrepareFrame(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

        if (MatchCommandApplicator.Apply(_match, TinyGame.Commands))
        {
            ReturnToMenu();
            return;
        }

        // ReplaceScreen may unload this screen (e.g. if Apply somehow nested).
        if (_match is null)
            return;

        ApplyPointerIfAny();

        if (_match is null)
            return;

        _match.Update(gameTime);
        SyncHud();
        GumService.Default.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 20, 28));

        if (_match is not null)
        {
            _match.PrepareFrame(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _match.Draw(gameTime);
            _cursorHighlight?.Draw(
                TinyGame.SharedSpriteBatch,
                _match.Layout,
                _match.Cursor,
                hasSelection: _match.SelectedEntityId is not null);
        }

        GumService.Default.Draw();
    }

    private void ApplyPointerIfAny()
    {
        if (_match is null)
            return;

        var mouse = Mouse.GetState();
        if (mouse.LeftButton != ButtonState.Pressed)
            return;

        if (_match.Layout.TryScreenToCell(mouse.Position.ToVector2(), out var cell))
            MatchCommandApplicator.ApplyPointer(_match, cell);
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
