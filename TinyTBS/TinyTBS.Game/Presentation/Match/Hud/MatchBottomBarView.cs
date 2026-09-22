using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Hud;

public sealed class MatchBottomBarView
{
    public const float MenuButtonWidth = 88f;

    private Label? _hintLabel;
    private Button? _menuButton;

    public Button? MenuButton => _menuButton;

    public void Build(Panel root, GameplayHudViewModel hud, Action onOpenPause)
    {
        var bottomBar = new Panel();
        bottomBar.Dock(Dock.Bottom);
        bottomBar.Visual.Height = 52;
        bottomBar.Visual.HeightUnits = DimensionUnitType.Absolute;
        bottomBar.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        bottomBar.Visual.Width = 0;
        bottomBar.Visual.HasEvents = false;
        root.AddChild(bottomBar);

        GumUiLayout.AddSolidBackground(bottomBar, MatchUiColors.BottomBar);

        _hintLabel = new Label { Text = hud.HintText };
        _hintLabel.Dock(Dock.Fill);
        _hintLabel.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        _hintLabel.Visual.Width = -(MenuButtonWidth + 24f);
        bottomBar.AddChild(_hintLabel);

        var menuButton = new Button { Text = "Menu" };
        menuButton.Anchor(Anchor.Right);
        menuButton.X = -12;
        menuButton.Visual.Width = MenuButtonWidth;
        menuButton.Visual.WidthUnits = DimensionUnitType.Absolute;
        menuButton.Click += (_, _) => onOpenPause();
        bottomBar.AddChild(menuButton);
        _menuButton = menuButton;
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_hintLabel is not null)
            _hintLabel.Text = hud.HintText;
    }

    public void SetMenuEnabled(bool enabled)
    {
        if (_menuButton is not null)
            _menuButton.IsEnabled = enabled;
    }

    public void ClearMenuFocus()
    {
        if (_menuButton is { IsFocused: true })
            _menuButton.IsFocused = false;
    }
}
