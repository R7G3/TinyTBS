using Microsoft.Xna.Framework;
using TinyTBS.Engine.Diagnostics;
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
    private bool _awaitingNewGameAbandonConfirm;

    public MainMenuScreen(GameMain game)
        : base(game)
    {
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
        var hasDiskSave = App.Saves.TryGetLatest(out _);
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

            Navigator.ShowMatch(session);
            return;
        }

        try
        {
            if (!App.Saves.TryGetLatest(out var entry))
                throw new MatchSaveException("No saves found.");

            Navigator.StartMatch(App.Saves.CreateResumeRequest(entry));
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
        Navigator.ToNewGame();
    }

    private void OpenLoadGame()
    {
        _awaitingNewGameAbandonConfirm = false;
        _viewModel.StatusHint = string.Empty;
        Navigator.ToLoadGame();
    }

    private void OpenContent() =>
        Navigator.ToContentLibrary();

    private void OpenEditor() =>
        Navigator.ToEditorHub();

    private void OpenAbout() =>
        Navigator.ToAbout();
}
