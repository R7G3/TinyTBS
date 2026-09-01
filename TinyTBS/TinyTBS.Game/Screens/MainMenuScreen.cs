using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Screens;
using TinyTBS.Core.Assets;
using TinyTBS.Game.Gum;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

public sealed class MainMenuScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly MainMenuViewModel _viewModel = new();

    private Panel? _rootPanel;
    private ComboBox? _modComboBox;
    private Texture2D? _placeholderTexture;

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

        try
        {
            using var stream = TitleContainer.OpenStream("Content/Images/placeholder.png");
            _placeholderTexture = Texture2D.FromStream(GraphicsDevice, stream);
        }
        catch
        {
            _placeholderTexture = null;
        }

        BuildUi();
    }

    public override void UnloadContent()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _modComboBox = null;
        _placeholderTexture?.Dispose();
        _placeholderTexture = null;

        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)
            || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Game.Exit();
            return;
        }

        GumService.Default.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 28, 38));
        DrawBackground();

        GumService.Default.Draw();
    }

    private void DrawBackground()
    {
        if (_placeholderTexture is null)
            return;

        var batch = TinyGame.SharedSpriteBatch;
        var viewport = GraphicsDevice.Viewport;
        var scale = Math.Min(
            (float)viewport.Width / _placeholderTexture.Width,
            (float)viewport.Height / _placeholderTexture.Height);

        var drawSize = new Vector2(_placeholderTexture.Width, _placeholderTexture.Height) * scale;
        var position = new Vector2(
            (viewport.Width - drawSize.X) * 0.5f,
            (viewport.Height - drawSize.Y) * 0.5f);

        batch.Begin(SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend);
        batch.Draw(_placeholderTexture, new Rectangle(
            (int)position.X,
            (int)position.Y,
            (int)drawSize.X,
            (int)drawSize.Y), Color.White * 0.35f);
        batch.End();
    }

    private void BuildUi()
    {
        GumService.Default.Root.Children.Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var bodyPanel = new Panel();
        bodyPanel.Dock(Dock.Fill);
        _rootPanel.AddChild(bodyPanel);

        var contentPanel = GumUiLayout.CreateVerticalStackPanel(spacing: 14f, widthPercent: 90f);
        GumUiLayout.CenterInParent(contentPanel, xPercent: 50f, yPercent: 45f);
        bodyPanel.AddChild(contentPanel);

        var title = new Label { Text = _viewModel.Title };
        GumUiLayout.FillParentWidth(title);
        contentPanel.AddChild(title);

        var modLabel = new Label { Text = "Graphics mod" };
        GumUiLayout.FillParentWidth(modLabel);
        contentPanel.AddChild(modLabel);

        _modComboBox = new ComboBox
        {
            Items = _viewModel.ModOptions.ToList()
        };
        GumUiLayout.FillParentWidth(_modComboBox);

        var selectedIndex = _viewModel.ModOptions
            .ToList()
            .FindIndex(option => option == _viewModel.SelectedModOption);
        _modComboBox.SelectedIndex = Math.Max(selectedIndex, 0);
        _modComboBox.SelectionChanged += OnModSelectionChanged;
        contentPanel.AddChild(_modComboBox);

        var hint = new Label
        {
            Text = "Vanilla uses bundled content. Mods load from Mods/ next to the game."
        };
        GumUiLayout.FillParentWidth(hint);
        contentPanel.AddChild(hint);

        var exitButton = new Button { Text = "Exit" };
        GumUiLayout.PinToBottomRight(exitButton, insetPixels: 24f, widthPercent: 14f);
        exitButton.Click += (_, _) => Game.Exit();
        _rootPanel.AddChild(exitButton);

        _modComboBox.IsFocused = true;
    }

    private void OnModSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_modComboBox is null)
            return;

        var selected = _modComboBox.SelectedObject as string
            ?? _modComboBox.Text;
        if (string.IsNullOrWhiteSpace(selected))
            return;

        _viewModel.SelectedModOption = selected;
        _viewModel.ApplyModSelection(_assets);
    }
}
