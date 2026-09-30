using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Editor.Screens;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Menu;
using TinyTBS.Game.Saves;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Thin frame glue: wires menu UI-state, Gum presentation, and background draw.
/// </summary>
public sealed class MainMenuScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly MainMenuViewModel _viewModel = new();
    private readonly MainMenuView _view = new();
    private readonly SaveCatalog _saveCatalog;

    private MainMenuBackground? _background;
    private bool _awaitingNewGameAbandonConfirm;

    public MainMenuScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
        _saveCatalog = new SaveCatalog(game.UserDataPaths);
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        RefreshContinueAndLoadState();
        _viewModel.CanOpenContent = true;
        _viewModel.CanOpenEditor = true;
        _viewModel.CanOpenSettings = false;
        _viewModel.CanOpenAbout = true;
        _viewModel.CanStartNewGame = true;

        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);

        _view.Build(
            _viewModel,
            onContinue: ContinueGame,
            onNewGame: StartNewGame,
            onLoadGame: OpenLoadGame,
            onContent: OpenContent,
            onEditor: OpenEditor,
            onSettings: () => { },
            onAbout: OpenAbout,
            onExit: () => Game.Exit());
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
        _view.HandleInput(TinyGame.Commands, (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info))
        {
            if (_awaitingNewGameAbandonConfirm)
            {
                _awaitingNewGameAbandonConfirm = false;
                _viewModel.StatusHint = string.Empty;
                _view.SyncStatus(_viewModel);
                return;
            }

            Game.Exit();
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 28, 38));

        _background?.Draw(
            TinyGame.SharedSpriteBatch,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height,
            gameTime);

        GumService.Default.Draw();
    }

    private void RefreshContinueAndLoadState()
    {
        var hasDiskSave = _saveCatalog.TryGetLatest(out _);
        _viewModel.CanContinue = TinyGame.HasSuspendedMatch || hasDiskSave;
        _viewModel.CanLoadGame = hasDiskSave;
    }

    private void ContinueGame()
    {
        _awaitingNewGameAbandonConfirm = false;
        _viewModel.StatusHint = string.Empty;

        if (TinyGame.HasSuspendedMatch)
        {
            var session = TinyGame.TakeSuspendedMatch();
            if (session is null)
                return;

            ScreenManager.ReplaceScreen(new GameplayScreen(TinyGame, _assets, session));
            return;
        }

        try
        {
            if (!_saveCatalog.TryGetLatest(out var entry))
                throw new MatchSaveException("No saves found.");

            if (entry.IsCampaign)
            {
                var progress = _saveCatalog.CampaignStore.ReadFile(entry.FilePath);
                var request = CampaignRunRestorer.CreateChapterStartRequest(progress);
                ScreenManager.ReplaceScreen(new LoadingScreen(TinyGame, _assets, request));
                return;
            }

            var document = _saveCatalog.MatchLibrary.Load(entry.FilePath);
            var matchRequest = MatchSaveResume.CreateRequest(
                document,
                TinyGame.Files,
                TinyGame.UserDataPaths);
            ScreenManager.ReplaceScreen(new LoadingScreen(TinyGame, _assets, matchRequest));
        }
        catch (Exception exception)
        {
            _viewModel.StatusHint = "Continue failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
            RefreshContinueAndLoadState();
        }
    }

    private void StartNewGame()
    {
        if (TinyGame.HasSuspendedMatch)
        {
            if (!_awaitingNewGameAbandonConfirm)
            {
                _awaitingNewGameAbandonConfirm = true;
                _viewModel.StatusHint = "Confirm New Game again to leave the current match. Back cancels.";
                _view.SyncStatus(_viewModel);
                return;
            }

            TinyGame.ClearSuspendedMatch(dispose: true);
        }

        _awaitingNewGameAbandonConfirm = false;
        _viewModel.StatusHint = string.Empty;
        ScreenManager.ReplaceScreen(new NewGameScreen(TinyGame, _assets));
    }

    private void OpenLoadGame()
    {
        _awaitingNewGameAbandonConfirm = false;
        _viewModel.StatusHint = string.Empty;
        ScreenManager.ReplaceScreen(new LoadGameScreen(TinyGame, _assets));
    }

    private void OpenContent() =>
        ScreenManager.ReplaceScreen(new ContentLibraryScreen(TinyGame, _assets));

    private void OpenEditor() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets));

    private void OpenAbout() =>
        ScreenManager.ReplaceScreen(new AboutScreen(TinyGame, _assets));
}
