using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation;

/// <summary>
/// Presentation: Gum HUD for the gameplay screen.
/// </summary>
public sealed class GameplayHudView
{
    private Panel? _rootPanel;
    private Label? _statusLabel;

    public void Build(GameplayHudViewModel hud, Action onEndTurn, Action onMenu)
    {
        Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var topBar = new Panel();
        topBar.Dock(Dock.Top);
        topBar.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        topBar.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        topBar.Visual.Width = 0;
        _rootPanel.AddChild(topBar);

        _statusLabel = new Label { Text = hud.StatusText };
        GumUiLayout.FillParentWidth(_statusLabel);
        topBar.AddChild(_statusLabel);

        var hint = new Label { Text = hud.HintText };
        GumUiLayout.FillParentWidth(hint);
        topBar.AddChild(hint);

        var endTurnButton = new Button { Text = "End turn" };
        endTurnButton.Click += (_, _) => onEndTurn();
        topBar.AddChild(endTurnButton);

        var menuButton = new Button { Text = "Menu" };
        GumUiLayout.PinToBottomRight(menuButton, insetPixels: 24f, widthPercent: 14f);
        menuButton.Click += (_, _) => onMenu();
        _rootPanel.AddChild(menuButton);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_statusLabel is null)
            return;

        _statusLabel.Text = hud.StatusText;
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _statusLabel = null;
    }
}
