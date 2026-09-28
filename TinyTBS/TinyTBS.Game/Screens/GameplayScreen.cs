using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation.Match;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>Thin frame glue: match session lifecycle via <see cref="GameplayMatchController"/>.</summary>
public sealed class GameplayScreen : GameScreen
{
    private readonly GameplayHudViewModel _hud = new();
    private readonly GameplayHudComposer _hudComposer = new();
    private readonly GameplaySession _session;
    private GameplayMatchController? _controller;
    private bool _sessionTransferred;

    public GameplayScreen(GameMain game, IAssetResolver assets, GameplaySession session)
        : base(game)
    {
        ArgumentNullException.ThrowIfNull(session);
        Assets = assets;
        _session = session;
    }

    private IAssetResolver Assets { get; }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        _controller = new GameplayMatchController(
            TinyGame,
            GraphicsDevice,
            _hud,
            _hudComposer);
        _controller.LoadContent(
            _session,
            onSuspendToMenu: SuspendToMainMenu,
            onLeaveMatch: LeaveMatchToMainMenu,
            onSaveMatch: SaveMatch,
            onLoadMatch: SuspendAndOpenLoadGame);
    }

    public override void UnloadContent()
    {
        _controller?.UnloadContent(disposeSession: !_sessionTransferred);
        _controller = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime) =>
        _controller?.Update(gameTime);

    public override void Draw(GameTime gameTime) =>
        _controller?.Draw(gameTime);

    private void SuspendToMainMenu()
    {
        _sessionTransferred = true;
        TinyGame.SuspendMatch(_session);
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, Assets));
    }

    private void LeaveMatchToMainMenu()
    {
        _sessionTransferred = false;
        TinyGame.ClearSuspendedMatch(dispose: true);
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, Assets));
    }

    private void SuspendAndOpenLoadGame()
    {
        _sessionTransferred = true;
        TinyGame.SuspendMatch(_session);
        ScreenManager.ReplaceScreen(new LoadGameScreen(TinyGame, Assets));
    }

    private void SaveMatch()
    {
        _controller?.SaveCurrentMatch();
    }
}
