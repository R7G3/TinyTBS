using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Core.Assets;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Thin frame glue: wires match session, HUD presentation, and per-frame update/draw.
/// </summary>
public sealed class GameplayScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly GameplayHudViewModel _hud = new();
    private readonly GameplayHudView _hudView = new();

    private GameplaySession? _session;

    public GameplayScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        _session = GameplaySessionFactory.CreateDemo(
            GraphicsDevice,
            Content,
            TinyGame.SharedSpriteBatch,
            _assets);

        _hudView.Build(
            _hud,
            onEndTurn: () => _session?.State.EndTurn(),
            onMenu: ReturnToMenu);
        SyncHud();
    }

    public override void UnloadContent()
    {
        _hudView.Clear();
        _session?.Dispose();
        _session = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        if (_session is null)
            return;

        var match = _session.State;
        var scene = _session.Scene;

        scene.PrepareFrame(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

        if (MatchCommandApplicator.Apply(match, TinyGame.Commands))
        {
            ReturnToMenu();
            return;
        }

        if (_session is null)
            return;

        MatchCommandApplicator.ApplyPointer(match, TinyGame.Pointer, scene.Layout);

        if (_session is null)
            return;

        scene.Update(gameTime);
        SyncHud();
        GumService.Default.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 20, 28));

        if (_session is not null)
        {
            var match = _session.State;
            var scene = _session.Scene;
            scene.PrepareFrame(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            scene.Draw(gameTime);
            _session.CursorHighlight.Draw(
                TinyGame.SharedSpriteBatch,
                scene.Layout,
                match.Cursor,
                hasSelection: match.SelectedUnitId is not null);
        }

        GumService.Default.Draw();
    }

    private void SyncHud()
    {
        if (_session is null)
            return;

        _hud.StatusText = _session.State.StatusText;
        _hudView.Sync(_hud);
    }

    private void ReturnToMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, _assets));
}
