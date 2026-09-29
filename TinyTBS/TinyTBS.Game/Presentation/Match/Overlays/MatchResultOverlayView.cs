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
    private Button? _nextChapterButton;
    private Button? _retryButton;
    private Button? _menuButton;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;
    private Action? _onReturnToMenu;
    private Action? _onNextChapter;
    private Action? _onRetry;

    public void Build(
        Panel root,
        Action onReturnToMenu,
        Action? onNextChapter = null,
        Action? onRetry = null)
    {
        _onReturnToMenu = onReturnToMenu;
        _onNextChapter = onNextChapter;
        _onRetry = onRetry;
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;

        _panel = GumMatchOverlayPanel.Create(
            root,
            maxWidthPixels: 360f,
            centerXPercent: 50f,
            centerYPercent: 48f,
            UiColors.MatchOverlayDark);
        var stack = GumMatchOverlayPanel.AddContentStack(_panel, spacing: 12f);

        GumUiLayout.AddVerticalSpacer(stack, 16f);

        _titleLabel = new Label { Text = "Match over" };
        GumUiLayout.FillParentWidth(_titleLabel);
        stack.AddChild(_titleLabel);

        _nextChapterButton = new Button { Text = "Next chapter", IsEnabled = false };
        _nextChapterButton.Visual.HasEvents = false;
        GumUiLayout.FillParentWidth(_nextChapterButton);
        _nextChapterButton.Click += (_, _) => _onNextChapter?.Invoke();
        stack.AddChild(_nextChapterButton);

        _retryButton = new Button { Text = "Retry", IsEnabled = false };
        _retryButton.Visual.HasEvents = false;
        GumUiLayout.FillParentWidth(_retryButton);
        _retryButton.Click += (_, _) => _onRetry?.Invoke();
        stack.AddChild(_retryButton);

        _menuButton = new Button { Text = "Main menu" };
        GumUiLayout.FillParentWidth(_menuButton);
        _menuButton.Click += (_, _) => _onReturnToMenu?.Invoke();
        stack.AddChild(_menuButton);

        GumUiLayout.AddVerticalSpacer(stack, 16f);
        RefreshFocusables(showNext: false, showRetry: false);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_titleLabel is not null && !string.IsNullOrEmpty(hud.MatchResultText))
            _titleLabel.Text = hud.MatchResultText;

        if (hud.IsMatchResultVisible)
            ApplyActionVisibility(hud.ShowMatchResultNextChapter, hud.ShowMatchResultRetry);

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

    private void ApplyActionVisibility(bool showNext, bool showRetry)
    {
        SetButtonActive(_nextChapterButton, showNext && _onNextChapter is not null);
        SetButtonActive(_retryButton, showRetry && _onRetry is not null);
        RefreshFocusables(showNext && _onNextChapter is not null, showRetry && _onRetry is not null);
    }

    private static void SetButtonActive(Button? button, bool active)
    {
        if (button is null)
            return;

        button.IsEnabled = active;
        button.Visual.HasEvents = active;
        button.Visual.Visible = active;
    }

    private void RefreshFocusables(bool showNext, bool showRetry)
    {
        _focusableEntries.Clear();
        if (showNext && _nextChapterButton is not null && _onNextChapter is not null)
            _focusableEntries.Add((_nextChapterButton, _onNextChapter));
        if (showRetry && _retryButton is not null && _onRetry is not null)
            _focusableEntries.Add((_retryButton, _onRetry));
        if (_menuButton is not null && _onReturnToMenu is not null)
            _focusableEntries.Add((_menuButton, _onReturnToMenu));
    }
}
