using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.LoadGame;
using TinyTBS.Game.Saves;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>Match + campaign save list: load or delete. Back returns to the main menu.</summary>
public sealed class LoadGameScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly LoadGameViewModel _viewModel = new();
    private readonly LoadGameView _view = new();
    private readonly SaveCatalog _saveCatalog;

    private MainMenuBackground? _background;
    private LoadGameSaveRowViewModel? _pendingLoadRow;
    private bool _awaitingLoadAbandonConfirm;

    public LoadGameScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
        _saveCatalog = new SaveCatalog(game.UserDataPaths);
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);
        RefreshList("Confirm a save for Load / Delete. Back returns to the menu.");
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

        var detailWasOpen = _view.IsDetailOpen;
        _view.HandleInput(TinyGame.Commands, (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
        {
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

    private void RefreshList(string statusText)
    {
        var entries = _saveCatalog.ListNewestFirst();
        _viewModel.StatusText = statusText;
        _viewModel.Saves = entries
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
            var fileName = Path.GetFileName(row.FilePath);
            if (fileName.StartsWith("campaign_", StringComparison.OrdinalIgnoreCase))
            {
                var progress = _saveCatalog.CampaignStore.ReadFile(row.FilePath);
                var request = CampaignRunRestorer.CreateChapterStartRequest(progress);
                _view.CloseDetail();
                ScreenManager.ReplaceScreen(new LoadingScreen(TinyGame, _assets, request));
                return;
            }

            var document = _saveCatalog.MatchLibrary.Load(row.FilePath);
            var matchRequest = MatchSaveResume.CreateRequest(
                document,
                TinyGame.Files,
                TinyGame.UserDataPaths);
            _view.CloseDetail();
            ScreenManager.ReplaceScreen(new LoadingScreen(TinyGame, _assets, matchRequest));
        }
        catch (Exception exception)
        {
            _view.SetStatus("Load failed: " + exception.Message);
        }
    }

    private void DeleteSave(LoadGameSaveRowViewModel row)
    {
        _awaitingLoadAbandonConfirm = false;
        _pendingLoadRow = null;

        try
        {
            var fileName = Path.GetFileName(row.FilePath);
            if (fileName.StartsWith("campaign_", StringComparison.OrdinalIgnoreCase))
                _saveCatalog.CampaignStore.Delete(row.FilePath);
            else
                _saveCatalog.MatchLibrary.Delete(row.FilePath);

            _view.CloseDetail();
            RefreshList($"Deleted '{row.Title}'.");
        }
        catch (Exception exception)
        {
            _view.SetStatus("Delete failed: " + exception.Message);
        }
    }

    private void CancelAbandonConfirm()
    {
        _awaitingLoadAbandonConfirm = false;
        _pendingLoadRow = null;
        _view.SetStatus("Load cancelled. Confirm a save for Load / Delete.");
    }

    private void GoToMainMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, _assets));
}
