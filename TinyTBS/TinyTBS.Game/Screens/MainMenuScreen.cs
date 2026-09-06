using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Core.Assets;
using TinyTBS.Core.Input;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Presentation;
using TinyTBS.Game.Rendering;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Thin frame glue: wires menu UI-state, Gum presentation, and background fit draw.
/// </summary>
public sealed class MainMenuScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly MainMenuViewModel _viewModel = new();
    private readonly MainMenuView _view = new();

    private LoadedTexture? _placeholderAsset;

    public MainMenuScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        _viewModel.RefreshMods(_assets);
        _viewModel.ApplyModSelection(_assets);

        _placeholderAsset = GameTextureLoader.TryLoad(
            GraphicsDevice,
            Content,
            _assets,
            logicalRelativePath: "Images/placeholder.png",
            contentAssetName: "Images/placeholder");

        _view.Build(
            _viewModel,
            onStartMatch: StartMatch,
            onExit: () => Game.Exit(),
            onModSelectionChanged: OnModSelectionChanged);
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _placeholderAsset?.DisposeIfOwned();
        _placeholderAsset = null;

        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);

        if (TinyGame.Commands.WasPressed(GameCommand.Back))
            Game.Exit();
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 28, 38));

        var texture = _placeholderAsset?.Texture;
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

    private void StartMatch() =>
        ScreenManager.ReplaceScreen(new GameplayScreen(TinyGame, _assets));

    private void OnModSelectionChanged()
    {
        if (!_view.TryGetSelectedMod(out var selected))
            return;

        _viewModel.SelectedModOption = selected;
        _viewModel.ApplyModSelection(_assets);
    }
}
