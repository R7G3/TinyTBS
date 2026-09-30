using Microsoft.Xna.Framework;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Presentation.LoadGame;
using TinyTBS.Game.Saves;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>Match + campaign save list: load or delete. Back returns to the main menu.</summary>
public sealed class LoadGameScreen : MenuScreen
{
    private readonly LoadGameViewModel _viewModel = new();
    private readonly LoadGameView _view = new();
    private readonly SaveCatalog _saveCatalog;

    private IReadOnlyList<SaveCatalogEntry> _entries = [];
    private LoadGameSaveRowViewModel? _pendingLoadRow;
    private bool _awaitingLoadAbandonConfirm;

    public LoadGameScreen(GameMain game, IAssetResolver assets)
        : base(game, assets)
    {
        _saveCatalog = new SaveCatalog(game.Files, game.UserDataPaths);
    }

    protected override void OnLoad() =>
        RefreshList("Confirm a save for Load / Delete. Back returns to the menu.");

    protected override void OnUnload() => _view.Clear();

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        var detailWasOpen = _view.IsDetailOpen;
        _view.HandleInput(TinyGame.Commands, elapsedSeconds);

        if (!WasLeavePressed())
            return;

        if (_awaitingLoadAbandonConfirm)
        {
            CancelAbandonConfirm();
            return;
        }

        if (_view.IsDetailOpen)
            _view.TryCloseDetail();
        else if (!detailWasOpen)
            GoToMainMenu();
    }

    private void RefreshList(string statusText)
    {
        _entries = _saveCatalog.ListNewestFirst();
        _viewModel.StatusText = statusText;
        _viewModel.Saves = _entries
            .Select(entry => new LoadGameSaveRowViewModel
            {
                FilePath = entry.FilePath,
                Title = entry.Title,
                Meta = entry.Meta,
            })
            .ToList();

        _view.Build(
            _viewModel,
            onOpenSave: OpenSaveDetail,
            onBack: GoToMainMenu);
        _view.SetStatus(statusText);
    }

    private void OpenSaveDetail(LoadGameSaveRowViewModel row)
    {
        _awaitingLoadAbandonConfirm = false;
        _pendingLoadRow = null;
        _view.OpenDetail(
            row,
            onLoad: () => TryLoadSave(row),
            onDelete: () => DeleteSave(row));
    }

    private void TryLoadSave(LoadGameSaveRowViewModel row)
    {
        if (TinyGame.HasSuspendedMatch)
        {
            if (!_awaitingLoadAbandonConfirm || _pendingLoadRow?.FilePath != row.FilePath)
            {
                _awaitingLoadAbandonConfirm = true;
                _pendingLoadRow = row;
                _view.SetStatus("Confirm Load again to leave the current match. Back cancels.");
                return;
            }

            TinyGame.ClearSuspendedMatch(dispose: true);
        }

        _awaitingLoadAbandonConfirm = false;
        _pendingLoadRow = null;

        try
        {
            var request = _saveCatalog.CreateResumeRequest(FindEntry(row));
            _view.CloseDetail();
            ScreenManager.ReplaceScreen(new LoadingScreen(TinyGame, Assets, request));
        }
        catch (Exception exception)
        {
            GameLog.Error($"Loading save '{row.FilePath}' failed.", exception);
            _view.SetStatus("Load failed: " + exception.Message);
        }
    }

    private void DeleteSave(LoadGameSaveRowViewModel row)
    {
        _awaitingLoadAbandonConfirm = false;
        _pendingLoadRow = null;

        try
        {
            _saveCatalog.Delete(FindEntry(row));
            _view.CloseDetail();
            RefreshList($"Deleted '{row.Title}'.");
        }
        catch (Exception exception)
        {
            GameLog.Error($"Deleting save '{row.FilePath}' failed.", exception);
            _view.SetStatus("Delete failed: " + exception.Message);
        }
    }

    private SaveCatalogEntry FindEntry(LoadGameSaveRowViewModel row) =>
        _entries.FirstOrDefault(entry => entry.FilePath == row.FilePath)
        ?? throw new MatchSaveException($"Save '{row.Title}' is no longer listed.");

    private void CancelAbandonConfirm()
    {
        _awaitingLoadAbandonConfirm = false;
        _pendingLoadRow = null;
        _view.SetStatus("Load cancelled. Confirm a save for Load / Delete.");
    }

    private void GoToMainMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, Assets));
}
