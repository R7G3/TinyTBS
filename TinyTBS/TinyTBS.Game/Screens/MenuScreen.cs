using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Menu;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Base for menu-style screens: animated menu background behind a Gum view.
/// Derived screens build their view in <see cref="OnLoad"/> and react to input in <see cref="OnUpdate"/>
/// (called after Gum has processed the frame).
/// </summary>
public abstract class MenuScreen : GameScreen
{
    private MainMenuBackground? _background;

    protected MenuScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    protected GameMain TinyGame => (GameMain)Game;

    protected IAssetResolver Assets { get; }

    public sealed override void LoadContent()
    {
        base.LoadContent();
        _background = MainMenuBackground.Load(GraphicsDevice, Content, TinyGame.Files, Assets);
        OnLoad();
    }

    public sealed override void UnloadContent()
    {
        OnUnload();
        _background?.Dispose();
        _background = null;
        base.UnloadContent();
    }

    public sealed override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        OnUpdate(gameTime, (float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    public override void Draw(GameTime gameTime) => DrawBackdrop(gameTime);

    /// <summary>Clears the frame and draws the menu background and the Gum tree.</summary>
    protected void DrawBackdrop(GameTime gameTime)
    {
        GraphicsDevice.Clear(UiColors.MenuBackground);
        _background?.Draw(
            TinyGame.SharedSpriteBatch,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height,
            gameTime);
        GumService.Default.Draw();
    }

    /// <summary>Back / Cancel / Info, plus Pause when <paramref name="includePause"/> — the "leave this screen" inputs.</summary>
    protected bool WasLeavePressed(bool includePause = true)
    {
        var commands = TinyGame.Commands;
        return commands.WasPressed(GameCommand.Back)
            || commands.WasPressed(GameCommand.Cancel)
            || commands.WasPressed(GameCommand.Info)
            || (includePause && commands.WasPressed(GameCommand.Pause));
    }

    protected abstract void OnLoad();

    protected virtual void OnUnload()
    {
    }

    protected abstract void OnUpdate(GameTime gameTime, float elapsedSeconds);
}
