using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Overlays;

/// <summary>Shown when the match has a winner (standard or script victory).</summary>
public sealed class MatchResultOverlayView
{
    private Panel? _panel;
    private Label? _titleLabel;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;

    public void Build(Panel root, Action onReturnToMenu)
    {
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;

        _panel = GumMatchOverlayPanel.Create(
            root,
            maxWidthPixels: 360f,
            centerXPercent: 50f,
            centerYPercent: 48f,
            MatchUiColors.OverlayDark);
        var stack = GumMatchOverlayPanel.AddContentStack(_panel, spacing: 12f);

        GumUiLayout.AddVerticalSpacer(stack, 16f);

        _titleLabel = new Label { Text = "Match over" };
        GumUiLayout.FillParentWidth(_titleLabel);
        stack.AddChild(_titleLabel);

        var menuButton = new Button { Text = "Main menu" };
        GumUiLayout.FillParentWidth(menuButton);
        menuButton.Click += (_, _) => onReturnToMenu();
        stack.AddChild(menuButton);
        _focusableEntries.Add((menuButton, onReturnToMenu));

        GumUiLayout.AddVerticalSpacer(stack, 16f);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_titleLabel is not null && !string.IsNullOrEmpty(hud.MatchResultText))
            _titleLabel.Text = hud.MatchResultText;

        GumMatchVisibility.SetVisible(_panel, hud.IsMatchResultVisible);
    }

    public void HandleGamepadNavigation(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_panel is not { IsVisible: true } || _focusableEntries.Count == 0)
            return;

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _focusableEntries,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds);
    }

    public void FocusFirst()
    {
        _focusIndex = 0;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
    }

    public void ClearFocus()
    {
        GumFocusableButtonList.ClearFocus(_focusableEntries);
        _focusIndex = 0;
    }
}
