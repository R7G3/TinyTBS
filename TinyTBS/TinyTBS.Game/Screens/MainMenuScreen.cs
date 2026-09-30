using Microsoft.Xna.Framework;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Screens;
using TinyTBS.Game.Presentation.Menu;
using TinyTBS.Game.Saves;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Thin frame glue: wires menu UI-state and Gum presentation over the menu background.
/// </summary>
public sealed class MainMenuScreen : MenuScreen
{
    private readonly MainMenuViewModel _viewModel = new();
    private readonly MainMenuView _view = new();
    private readonly SaveCatalog _saveCatalog;

    private bool _awaitingNewGameAbandonConfirm;

    public MainMenuScreen(GameMain game, IAssetResolver assets)
        : base(game, assets)
    {
        _saveCatalog = new SaveCatalog(game.Files, game.UserDataPaths);
    }

    protected override void OnLoad()
    {
        RefreshContinueAndLoadState();
        _viewModel.CanOpenContent = true;
        _viewModel.CanOpenEditor = true;
        _viewModel.CanOpenSettings = false;
        _viewModel.CanOpenAbout = true;
        _viewModel.CanStartNewGame = true;

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

    protected override void OnUnload() => _view.Clear();

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        _view.HandleInput(TinyGame.Commands, elapsedSeconds);

        if (!WasLeavePressed(includePause: false))
            return;

        if (_awaitingNewGameAbandonConfirm)
        {
            _awaitingNewGameAbandonConfirm = false;
            _viewModel.StatusHint = string.Empty;
            _view.SyncStatus(_viewModel);
            return;
        }

        Game.Exit();
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

            ScreenManager.ReplaceScreen(new GameplayScreen(TinyGame, Assets, session));
            return;
        }

        try
        {
            if (!_saveCatalog.TryGetLatest(out var entry))
                throw new MatchSaveException("No saves found.");

            ScreenManager.ReplaceScreen(new LoadingScreen(TinyGame, Assets, _saveCatalog.CreateResumeRequest(entry)));
        }
        catch (Exception exception)
        {
            GameLog.Error("Continue from the latest save failed.", exception);
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
        ScreenManager.ReplaceScreen(new NewGameScreen(TinyGame, Assets));
    }

    private void OpenLoadGame()
    {
        _awaitingNewGameAbandonConfirm = false;
        _viewModel.StatusHint = string.Empty;
        ScreenManager.ReplaceScreen(new LoadGameScreen(TinyGame, Assets));
    }

    private void OpenContent() =>
        ScreenManager.ReplaceScreen(new ContentLibraryScreen(TinyGame, Assets));

    private void OpenEditor() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, Assets));

    private void OpenAbout() =>
        ScreenManager.ReplaceScreen(new AboutScreen(TinyGame, Assets));
}
