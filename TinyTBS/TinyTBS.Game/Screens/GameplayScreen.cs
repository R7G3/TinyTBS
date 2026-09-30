using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
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

    public GameplayScreen(GameMain game, GameplaySession session)
        : base(game)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(session);
        TinyGame = game;
        Navigator = game.Navigator;
        _session = session;
    }

    private GameMain TinyGame { get; }

    private ScreenNavigator Navigator { get; }

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
            onLoadMatch: SuspendAndOpenLoadGame,
            onNextChapter: StartCurrentCampaignChapter,
            onRetryChapter: StartCurrentCampaignChapter);
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
        Navigator.ToMainMenu();
    }

    private void LeaveMatchToMainMenu()
    {
        _sessionTransferred = false;
        TinyGame.ClearSuspendedMatch(dispose: true);
        Navigator.ToMainMenu();
    }

    private void SuspendAndOpenLoadGame()
    {
        _sessionTransferred = true;
        TinyGame.SuspendMatch(_session);
        Navigator.ToLoadGame();
    }

    private void SaveMatch()
    {
        _controller?.SaveCurrentMatch();
    }

    /// <summary>
    /// Next chapter and Retry both start <see cref="CampaignRunState.CurrentLevelId"/>:
    /// a won chapter has already advanced it, a lost one has not.
    /// </summary>
    private void StartCurrentCampaignChapter()
    {
        var run = _session.Runtime.CampaignRun;
        if (run is null)
            return;

        _sessionTransferred = false;
        TinyGame.ClearSuspendedMatch(dispose: true);

        Navigator.StartMatch(TinyGame.App.Campaigns.CreateChapterRequest(run));
    }
}
