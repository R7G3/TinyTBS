using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Input;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation.Menu;
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

    private MainMenuBackground? _background;

    public MainMenuScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        // Saves / content library / editor / settings / about arrive in later plan steps.
        _viewModel.CanContinue = false;
        _viewModel.CanLoadGame = false;
        _viewModel.CanOpenContent = false;
        _viewModel.CanOpenEditor = false;
        _viewModel.CanOpenSettings = false;
        _viewModel.CanOpenAbout = false;
        _viewModel.CanStartNewGame = true;

        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);

        _view.Build(
            _viewModel,
            onContinue: () => { },
            onNewGame: StartNewGame,
            onLoadGame: () => { },
            onContent: () => { },
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
        _view.HandleGamepadNavigation(TinyGame.Commands);

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info))
            Game.Exit();
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

    private void StartNewGame() =>
        ScreenManager.ReplaceScreen(
            new LoadingScreen(TinyGame, _assets, GameplaySessionFactory.ProvingGroundsLevelId));
}
