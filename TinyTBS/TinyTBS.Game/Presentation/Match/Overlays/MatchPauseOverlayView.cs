using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Overlays;

public sealed class MatchPauseOverlayView
{
    private Panel? _panel;
    private Button? _firstButton;
    private readonly List<Button> _buttons = [];

    public void Build(
        Panel root,
        Action onEndTurn,
        Action onOpenMinimap,
        Action onOpenGoals,
        Action onClosePause,
        Action onReturnToMenu)
    {
        _panel = GumMatchOverlayPanel.Create(root, widthPercent: 36f, centerXPercent: 50f, centerYPercent: 48f, MatchUiColors.OverlayDark);
        var stack = GumMatchOverlayPanel.AddContentStack(_panel, spacing: 10f);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var title = new Label { Text = "Pause" };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        _firstButton = AddButton(stack, "End turn", onEndTurn);
        AddButton(stack, "Map", onOpenMinimap);
        AddButton(stack, "Goals", onOpenGoals);

        var saveButton = new Button { Text = "Save (hotsit OK)" };
        saveButton.IsEnabled = false;
        GumUiLayout.FillParentWidth(saveButton);
        stack.AddChild(saveButton);

        var loadButton = new Button { Text = "Load (hotsit OK)" };
        loadButton.IsEnabled = false;
        GumUiLayout.FillParentWidth(loadButton);
        stack.AddChild(loadButton);

        AddButton(stack, "Resume", onClosePause);
        AddButton(stack, "Main menu", onReturnToMenu);

        GumUiLayout.AddVerticalSpacer(stack, 14f);
    }

    public void Sync(GameplayHudViewModel hud) =>
        GumMatchVisibility.SetVisible(_panel, hud.IsPauseVisible);

    public void FocusFirst()
    {
        if (_firstButton is not null)
            _firstButton.IsFocused = true;
    }

    public void ClearFocus()
    {
        foreach (var button in _buttons)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }
    }

    private Button AddButton(Panel stack, string text, Action onClick)
    {
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) => onClick();
        stack.AddChild(button);
        _buttons.Add(button);
        return button;
    }
}
