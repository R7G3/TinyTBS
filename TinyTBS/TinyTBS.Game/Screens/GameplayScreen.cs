using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Screens;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Presentation.Match;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>Thin frame glue: match session lifecycle via <see cref="GameplayMatchController"/>.</summary>
public sealed class GameplayScreen : GameScreen
{
    private readonly GameplayHudViewModel _hud = new();
    private readonly GameplayHudComposer _hudComposer = new();
    private GameplayMatchController? _controller;

    public GameplayScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        Assets = assets;
    }

    private IAssetResolver Assets { get; }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        _controller = new GameplayMatchController(
            TinyGame,
            Assets,
            GraphicsDevice,
            _hud,
            _hudComposer);
        _controller.LoadContent(() =>
            ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, Assets)));
    }

    public override void UnloadContent()
    {
        _controller?.UnloadContent();
        _controller = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime) =>
        _controller?.Update(gameTime);

    public override void Draw(GameTime gameTime) =>
        _controller?.Draw(gameTime);
}
