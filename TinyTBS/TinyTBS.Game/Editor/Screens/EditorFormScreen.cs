using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Menu;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>
/// Shared editor form screen: menu background, Gum update/draw, overlay/text-entry Back handling.
/// </summary>
public abstract class EditorFormScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private MainMenuBackground? _background;

    protected EditorFormScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    protected GameMain TinyGame => (GameMain)Game;

    protected IAssetResolver Assets => _assets;

    public override void LoadContent()
    {
        base.LoadContent();
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);
        OnFormLoad();
    }

    public override void UnloadContent()
    {
        OnFormUnload();
        _background?.Dispose();
        _background = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);

        var overlayWasOpen = IsOverlayOpen;
        HandleFormInput(
            TinyGame.Commands,
            TinyGame.Pointer,
            (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (overlayWasOpen || IsOverlayOpen)
            return;

        if (IsTextEntryActive)
            return;

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
        {
            OnBackRequested();
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

    protected abstract void OnFormLoad();

    protected abstract void OnFormUnload();

    protected abstract void HandleFormInput(
        IGameCommandSource commands,
        TinyTBS.Engine.Input.IPointerSource pointer,
        float elapsedSeconds);

    protected abstract bool IsOverlayOpen { get; }

    protected abstract bool IsTextEntryActive { get; }

    protected abstract void OnBackRequested();
}
