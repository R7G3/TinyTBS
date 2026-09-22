using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Hud;

public sealed class MatchCompactInfoView
{
    public const float PanelWidth = 220f;

    private Panel? _panel;
    private Label? _label;

    public void Build(Panel root, GameplayHudViewModel hud)
    {
        _panel = new Panel();
        _panel.Visual.Width = PanelWidth;
        _panel.Visual.WidthUnits = DimensionUnitType.Absolute;
        _panel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _panel.Visual.HasEvents = false;
        root.AddChild(_panel);

        GumUiLayout.AddSolidBackground(_panel, MatchUiColors.CompactInfo);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 4f, widthPercent: 92f);
        stack.Visual.HasEvents = false;
        GumUiLayout.CenterHorizontallyInParent(stack);
        _panel.AddChild(stack);

        GumUiLayout.AddVerticalSpacer(stack, 8f);

        _label = new Label { Text = hud.CompactInfoText };
        GumUiLayout.FillParentWidth(_label);
        stack.AddChild(_label);

        GumUiLayout.AddVerticalSpacer(stack, 8f);

        Place(hud);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_label is not null)
            _label.Text = hud.CompactInfoText;

        Place(hud);
        GumMatchVisibility.SetVisible(_panel, !hud.IsTileDetailVisible);
    }

    private void Place(GameplayHudViewModel hud)
    {
        if (_panel is null)
            return;

        var preferLeft = hud.InfoPreferLeft;
        var preferTop = hud.InfoPreferTop;

        if (preferLeft)
        {
            _panel.Anchor(preferTop ? Anchor.TopLeft : Anchor.BottomLeft);
            _panel.X = 12;
        }
        else
        {
            _panel.Anchor(preferTop ? Anchor.TopRight : Anchor.BottomRight);
            _panel.X = -12;
        }

        _panel.Y = preferTop ? 56 : -60;
    }
}
