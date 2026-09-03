using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Screens;
using TinyTBS.Core.Assets;
using TinyTBS.Core.Input;
using TinyTBS.Core.Match;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Gum;
using TinyTBS.Game.Match;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

public sealed class GameplayScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly GameplayHudViewModel _hud = new();

    private MatchSession? _match;
    private Panel? _rootPanel;
    private Label? _statusLabel;
    private Texture2D? _pixel;
    private LoadedTexture? _unitAsset;

    public GameplayScreen(GameMain game, IAssetResolver assets)
        : base(game)
    {
        _assets = assets;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        _unitAsset = GameTextureLoader.LoadOrFallback(
            GraphicsDevice,
            Content,
            _assets,
            logicalRelativePath: "Images/placeholder.png",
            contentAssetName: "Images/placeholder");
        _match = new MatchSession(GraphicsDevice, TinyGame.SharedSpriteBatch, _unitAsset.Value.Texture);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        BuildUi();
        SyncHud();
    }

    public override void UnloadContent()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _statusLabel = null;

        _match?.Dispose();
        _match = null;

        _pixel?.Dispose();
        _pixel = null;

        _unitAsset?.DisposeIfOwned();
        _unitAsset = null;

        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        if (_match is null)
            return;

        HandleInput();

        // ReplaceScreen unloads this screen inside HandleInput (e.g. Esc / Menu).
        if (_match is null)
            return;

        _match.Update(gameTime);
        SyncHud();

        GumService.Default.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 20, 28));

        _match?.Draw(gameTime);
        DrawCursorHighlight();

        GumService.Default.Draw();
    }

    private void HandleInput()
    {
        if (_match is null)
            return;

        var commands = TinyGame.Commands;

        if (commands.WasPressed(GameCommand.Back))
        {
            ReturnToMenu();
            return;
        }

        if (commands.WasPressed(GameCommand.EndTurn))
            _match.EndTurn();

        if (commands.WasPressed(GameCommand.Confirm))
            _match.HandleConfirm();

        if (commands.WasPressed(GameCommand.NavigateUp))
            _match.MoveCursor(0, -1);
        if (commands.WasPressed(GameCommand.NavigateDown))
            _match.MoveCursor(0, 1);
        if (commands.WasPressed(GameCommand.NavigateLeft))
            _match.MoveCursor(-1, 0);
        if (commands.WasPressed(GameCommand.NavigateRight))
            _match.MoveCursor(1, 0);

        var mouse = Mouse.GetState();
        if (mouse.LeftButton == ButtonState.Pressed
            && _match.Layout.TryScreenToCell(mouse.Position.ToVector2(), out var cell))
        {
            _match.HandlePointer(cell);
        }
    }

    private void DrawCursorHighlight()
    {
        if (_match is null || _pixel is null)
            return;

        _match.Layout.UpdateForViewport(
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height);

        var cell = _match.Cursor;
        var topLeft = _match.Layout.Origin + new Vector2(cell.X * _match.Layout.TileSize, cell.Y * _match.Layout.TileSize);
        var rect = new Rectangle((int)topLeft.X, (int)topLeft.Y, _match.Layout.TileSize, _match.Layout.TileSize);

        var batch = TinyGame.SharedSpriteBatch;
        batch.Begin();
        batch.Draw(_pixel, rect, Color.White * 0.18f);

        var borderColor = _match.SelectedEntityId is null
            ? new Color(255, 220, 80)
            : new Color(120, 255, 160);
        DrawRectBorder(batch, _pixel, rect, borderColor, thickness: 2);

        batch.End();
    }

    private static void DrawRectBorder(SpriteBatch batch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        batch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        batch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        batch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        batch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void BuildUi()
    {
        GumService.Default.Root.Children.Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var topBar = new Panel();
        topBar.Dock(Dock.Top);
        topBar.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        topBar.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        topBar.Visual.Width = 0;
        _rootPanel.AddChild(topBar);

        _statusLabel = new Label { Text = _hud.StatusText };
        GumUiLayout.FillParentWidth(_statusLabel);
        topBar.AddChild(_statusLabel);

        var hint = new Label { Text = _hud.HintText };
        GumUiLayout.FillParentWidth(hint);
        topBar.AddChild(hint);

        var endTurnButton = new Button { Text = "End turn" };
        endTurnButton.Click += (_, _) => _match?.EndTurn();
        topBar.AddChild(endTurnButton);

        var menuButton = new Button { Text = "Menu" };
        GumUiLayout.PinToBottomRight(menuButton, insetPixels: 24f, widthPercent: 14f);
        menuButton.Click += (_, _) => ReturnToMenu();
        _rootPanel.AddChild(menuButton);
    }

    private void SyncHud()
    {
        if (_match is null || _statusLabel is null)
            return;

        _hud.StatusText = _match.StatusText;
        _statusLabel.Text = _hud.StatusText;
    }

    private void ReturnToMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, _assets));
}
