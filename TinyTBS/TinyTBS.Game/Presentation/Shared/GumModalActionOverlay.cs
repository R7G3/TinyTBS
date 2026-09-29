using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Presentation.Shared;

/// <summary>
/// Configurable modal card: scrim + title/meta/body + action buttons.
/// Used by Editor choice/detail, Content library detail, Load Game detail.
/// </summary>
public sealed class GumModalActionOverlay
{
    private Panel? _rootPanel;
    private Panel? _overlay;
    private readonly List<(Button Button, Action Activate)> _focusable = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;
    private Action? _onClosed;
    private bool _useVerticalNavigation;
    private bool _navigateHorizontally = true;

    public bool IsOpen => _overlay is not null;

    public Color ScrimColor { get; set; } = UiColors.MenuDetailScrim;

    public Color CardColor { get; set; } = UiColors.MenuPanel;

    public float MaxCardWidthPixels { get; set; } = 480f;

    /// <summary>
    /// Opens the overlay. <paramref name="dismissLabel"/> closes without invoking an action
    /// (Cancel / Back / Close). Pass <see langword="null"/> to omit a dismiss button.
    /// </summary>
    public void Open(
        Panel rootPanel,
        string title,
        string? metaLine,
        string? bodyLine,
        IReadOnlyList<(string Label, Action Activate)> actions,
        string? dismissLabel,
        Action? onClosed,
        bool preferVerticalActions = false,
        int initialFocusIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(rootPanel);
        ArgumentNullException.ThrowIfNull(actions);

        Close(notifyClosed: false);
        _rootPanel = rootPanel;
        _onClosed = onClosed;

        _overlay = new Panel();
        _overlay.Dock(Dock.Fill);
        rootPanel.AddChild(_overlay);

        var scrim = new Panel();
        scrim.Dock(Dock.Fill);
        _overlay.AddChild(scrim);
        GumUiLayout.AddSolidBackground(scrim, ScrimColor);

        var card = new Panel();
        GumUiLayout.CenterInParent(card, xPercent: 50f, yPercent: 50f);
        GumUiLayout.SetBoundedWidth(card, maxPixels: MaxCardWidthPixels, parentPercent: 90f);
        card.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _overlay.AddChild(card);
        GumUiLayout.AddSolidBackground(card, CardColor);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 8f, widthPercent: 92f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        card.AddChild(stack);
        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var titleLabel = new Label { Text = title };
        GumUiLayout.FillParentWidth(titleLabel);
        stack.AddChild(titleLabel);

        if (!string.IsNullOrWhiteSpace(metaLine))
        {
            var meta = new Label { Text = metaLine };
            GumUiLayout.FillParentWidth(meta);
            stack.AddChild(meta);
        }

        if (!string.IsNullOrWhiteSpace(bodyLine))
        {
            var body = new Label { Text = bodyLine };
            GumUiLayout.FillParentWidth(body);
            stack.AddChild(body);
        }

        var actionsHost = new Panel();
        GumUiLayout.FillParentWidth(actionsHost);
        actionsHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.AddChild(actionsHost);

        var detailButtons = new List<Button>();
        foreach (var (label, activate) in actions)
        {
            var captured = activate;
            var button = new Button { Text = label };
            button.Click += (_, _) =>
            {
                Close(notifyClosed: false);
                captured();
            };
            detailButtons.Add(button);
            _focusable.Add((button, () =>
            {
                Close(notifyClosed: false);
                captured();
            }));
        }

        if (!string.IsNullOrWhiteSpace(dismissLabel))
        {
            var dismissButton = new Button { Text = dismissLabel };
            dismissButton.Click += (_, _) => Close();
            detailButtons.Add(dismissButton);
            _focusable.Add((dismissButton, () => Close()));
        }

        _useVerticalNavigation = preferVerticalActions || actions.Count > 4;
        _navigateHorizontally = !_useVerticalNavigation;
        if (_useVerticalNavigation)
        {
            actionsHost.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
            actionsHost.Visual.StackSpacing = 8f;
            foreach (var button in detailButtons)
            {
                GumUiLayout.FillParentWidth(button);
                actionsHost.AddChild(button);
            }
        }
        else
        {
            var canvasWidth = GumService.Default.CanvasWidth;
            var cardInner = Math.Min(MaxCardWidthPixels, canvasWidth > 0 ? canvasWidth * 0.9f : MaxCardWidthPixels) * 0.92f;
            GumUiLayout.LayoutAdaptiveButtonRows(
                actionsHost,
                detailButtons,
                cardInner,
                spacing: 8f,
                minButtonWidth: 88f,
                preferredButtonWidth: 140f);
        }

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        _focusIndex = Math.Clamp(initialFocusIndex, 0, Math.Max(0, _focusable.Count - 1));
        if (_focusable.Count > 0)
            GumFocusableButtonList.ApplyFocus(_focusable, ref _focusIndex);
    }

    public bool TryClose()
    {
        if (!IsOpen)
            return false;
        Close();
        return true;
    }

    public void Close(bool notifyClosed = true)
    {
        if (_overlay is null)
        {
            _focusable.Clear();
            _focusIndex = 0;
            _rootPanel = null;
            _onClosed = null;
            return;
        }

        if (_rootPanel is not null)
        {
            for (var i = _rootPanel.Visual.Children.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_rootPanel.Visual.Children[i], _overlay.Visual))
                {
                    _rootPanel.Visual.Children.RemoveAt(i);
                    break;
                }
            }
        }

        _overlay.Visual.Parent = null;
        _overlay.IsVisible = false;
        _overlay = null;
        _focusable.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;
        _useVerticalNavigation = false;
        _navigateHorizontally = true;
        _rootPanel = null;
        var onClosed = _onClosed;
        _onClosed = null;
        if (notifyClosed)
            onClosed?.Invoke();
    }

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_overlay is null || _focusable.Count == 0)
            return;

        if (commands.WasPressed(GameCommand.Cancel)
            || commands.WasPressed(GameCommand.Back)
            || commands.WasPressed(GameCommand.Info)
            || commands.WasPressed(GameCommand.Pause))
        {
            Close();
            return;
        }

        if (_navigateHorizontally)
        {
            GumFocusableButtonList.HandleHorizontalInput(
                commands,
                _focusable,
                ref _focusIndex,
                _navigateRepeat,
                elapsedSeconds);
        }
        else
        {
            GumFocusableButtonList.HandleVerticalInput(
                commands,
                _focusable,
                ref _focusIndex,
                _navigateRepeat,
                elapsedSeconds);
        }
    }
}
