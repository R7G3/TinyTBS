using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Overlays;

public sealed class MatchGoalsOverlayView
{
    private Panel? _panel;
    private Label? _goalsLabel;

    public void Build(Panel root, GameplayHudViewModel hud)
    {
        _panel = GumMatchOverlayPanel.Create(root, maxWidthPixels: 360f, centerXPercent: 50f, centerYPercent: 55f, MatchUiColors.OverlayShop);
        var stack = GumMatchOverlayPanel.AddContentStack(_panel, spacing: 10f);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var title = new Label { Text = "Goals" };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        _goalsLabel = new Label { Text = hud.GoalsText };
        GumUiLayout.FillParentWidth(_goalsLabel);
        stack.AddChild(_goalsLabel);

        GumUiLayout.AddVerticalSpacer(stack, 14f);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_goalsLabel is not null)
            _goalsLabel.Text = hud.GoalsText;
        GumMatchVisibility.SetVisible(_panel, hud.IsGoalsVisible);
    }
}
