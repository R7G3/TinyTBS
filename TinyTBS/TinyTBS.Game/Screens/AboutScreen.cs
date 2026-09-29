using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.About;
using TinyTBS.Game.Presentation.Menu;

namespace TinyTBS.Game.Screens;

/// <summary>Credits, project links, and solution library list.</summary>
public sealed class AboutScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly AboutView _view = new();
    private MainMenuBackground? _background;

    public AboutScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);
        _view.Build(GoToMainMenu, OpenUrl);
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
        _view.HandleInput(
            TinyGame.Commands,
            (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
        {
            GoToMainMenu();
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

    private void OpenUrl(string url)
    {
        if (TinyGame.UriLauncher.TryOpen(url, out var error))
        {
            _view.SyncStatus(string.Empty);
            return;
        }

        _view.SyncStatus("Open link failed: " + (error ?? "unknown error"));
    }

    private void GoToMainMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, _assets));
}
