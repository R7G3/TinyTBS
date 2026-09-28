using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Input;
using TinyTBS.Game.Match;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Presentation.Menu;
using TinyTBS.Game.Saves;
using TinyTBS.Game.Saves.Models;
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
    private readonly MatchSaveLibrary _saveLibrary;

    private MainMenuBackground? _background;
    private bool _awaitingNewGameAbandonConfirm;

    public MainMenuScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
        _saveLibrary = new MatchSaveLibrary(game.UserDataPaths);
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        RefreshContinueState();
        _viewModel.CanLoadGame = false;
        _viewModel.CanOpenContent = true;
        _viewModel.CanOpenEditor = false;
        _viewModel.CanOpenSettings = false;
        _viewModel.CanOpenAbout = false;
        _viewModel.CanStartNewGame = true;

        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);

        _view.Build(
            _viewModel,
            onContinue: ContinueGame,
            onNewGame: StartNewGame,
            onLoadGame: () => { },
            onContent: OpenContent,
            onEditor: () => { },
            onSettings: () => { },
            onAbout: () => { },
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

        var texture = _background?.Texture;
        if (texture is not null)
        {
            ViewportFit.DrawCentered(
                TinyGame.SharedSpriteBatch,
                texture,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                Color.White * 0.35f);
        }

        GumService.Default.Draw();
    }

    private void RefreshContinueState()
    {
        _viewModel.CanContinue = TinyGame.HasSuspendedMatch || _saveLibrary.TryGetLatest(out _);
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
            var document = _saveLibrary.LoadLatest();
            var warning = BuildVersionWarning(document);
            var request = ContinueMatchRequest.FromDocument(document);
            if (!string.IsNullOrWhiteSpace(warning))
            {
                request = new ContinueMatchRequest
                {
                    LevelId = request.LevelId,
                    ScenarioModuleId = request.ScenarioModuleId,
                    Composition = request.Composition,
                    PlayerCount = request.PlayerCount,
                    UnitCap = request.UnitCap,
                    PlayerSeats = request.PlayerSeats,
                    RuntimeSnapshot = request.RuntimeSnapshot,
                    VersionWarning = warning,
                };
            }

            ScreenManager.ReplaceScreen(new LoadingScreen(TinyGame, _assets, request));
        }
        catch (Exception exception)
        {
            _viewModel.StatusHint = "Continue failed: " + exception.Message;
            _view.SyncStatus(_viewModel);
            RefreshContinueState();
        }
    }

    private string? BuildVersionWarning(MatchSaveDocument document)
    {
        try
        {
            var current = MatchSaveDocumentFactory.CollectModuleVersions(
                MatchSaveDocumentFactory.ToComposition(document.ContentSetup),
                TinyGame.Files,
                TinyGame.UserDataPaths);

            foreach (var pair in document.ContentSetup.ModuleVersions)
            {
                if (!current.TryGetValue(pair.Key, out var now))
                    return $"Module '{pair.Key}' missing — attempting load…";
                if (!string.Equals(now, pair.Value, StringComparison.Ordinal))
                    return $"Module '{pair.Key}' version {pair.Value} → {now} — attempting load…";
            }
        }
        catch (MatchContentCompositionException)
        {
            return "Some modules changed — attempting load…";
        }

        return null;
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

    private void OpenContent()
    {
        if (TinyGame.HasSuspendedMatch)
        {
            // Content library keeps the suspended match; user can return via Continue.
        }

        ScreenManager.ReplaceScreen(new ContentLibraryScreen(TinyGame, _assets));
    }
}
