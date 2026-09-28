using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Overlays;

public sealed class MatchPauseOverlayView
{
    private Panel? _panel;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;

    public void Build(
        Panel root,
        Action onEndTurn,
        Action onOpenMinimap,
        Action onOpenGoals,
        Action onClosePause,
        Action onSaveMatch,
        Action onLoadMatch,
        Action onSuspendToMenu,
        Action onLeaveMatch)
    {
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;

        _panel = GumMatchOverlayPanel.Create(root, maxWidthPixels: 320f, centerXPercent: 50f, centerYPercent: 48f, MatchUiColors.OverlayDark);
        var stack = GumMatchOverlayPanel.AddContentStack(_panel, spacing: 10f);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var title = new Label { Text = "Pause" };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        AddButton(stack, "End turn", onEndTurn);
        AddButton(stack, "Map", onOpenMinimap);
        AddButton(stack, "Goals", onOpenGoals);
        AddButton(stack, "Save", onSaveMatch);
        AddButton(stack, "Load", onLoadMatch);
        AddButton(stack, "Resume", onClosePause);
        AddButton(stack, "Main menu", onSuspendToMenu);
        AddButton(stack, "Leave match", onLeaveMatch);

        GumUiLayout.AddVerticalSpacer(stack, 14f);
    }

    public void Sync(GameplayHudViewModel hud) =>
        GumMatchVisibility.SetVisible(_panel, hud.IsPauseVisible);

    /// <summary>Call after Gum.Update while pause is open. Owns D-pad / stick / Confirm.</summary>
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

    private void AddButton(Panel stack, string text, Action onClick)
    {
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) => onClick();
        stack.AddChild(button);
        _focusableEntries.Add((button, onClick));
    }
}
