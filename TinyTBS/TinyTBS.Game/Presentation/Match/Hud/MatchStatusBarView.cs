using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Hud;

public sealed class MatchStatusBarView
{
    private RectangleRuntime? _background;
    private Panel? _labelRow;
    private Label? _playerLabel;
    private Label? _goldLabel;
    private Label? _turnLabel;

    public void Build(Panel root, GameplayHudViewModel hud)
    {
        var statusBar = new Panel();
        statusBar.Dock(Dock.Top);
        statusBar.Visual.Height = 48;
        statusBar.Visual.HeightUnits = DimensionUnitType.Absolute;
        statusBar.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        statusBar.Visual.Width = 0;
        statusBar.Visual.HasEvents = false;
        root.AddChild(statusBar);

        _background = GumUiLayout.AddSolidBackground(statusBar, hud.StatusBarColor);

        _labelRow = new Panel();
        _labelRow.Dock(Dock.Fill);
        _labelRow.Visual.HasEvents = false;
        _labelRow.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        _labelRow.Visual.StackSpacing = ResolveLabelSpacing();
        statusBar.AddChild(_labelRow);

        _playerLabel = new Label { Text = hud.PlayerLabel };
        _labelRow.AddChild(_playerLabel);

        _goldLabel = new Label { Text = hud.GoldText };
        _labelRow.AddChild(_goldLabel);

        _turnLabel = new Label { Text = hud.TurnText };
        _labelRow.AddChild(_turnLabel);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_playerLabel is not null)
            _playerLabel.Text = hud.PlayerLabel;
        if (_goldLabel is not null)
            _goldLabel.Text = hud.GoldText;
        if (_turnLabel is not null)
            _turnLabel.Text = hud.TurnText;
        if (_background is not null)
            _background.FillColor = hud.StatusBarColor;
        if (_labelRow is not null)
            _labelRow.Visual.StackSpacing = ResolveLabelSpacing();
    }

    private static float ResolveLabelSpacing() =>
        GumService.Default.CanvasWidth is > 0 and < 520f ? 12f : 24f;
}
