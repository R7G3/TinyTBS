using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Presentation.Content;

/// <summary>Modal detail card over the content library shell (module/bundle info, optional Remove).</summary>
internal sealed class ContentLibraryDetailOverlay
{
    private Panel? _rootPanel;
    private Panel? _overlay;
    private readonly List<(Button Button, Action Activate)> _focusable = [];
    private int _focusIndex;
    private Action? _onClosed;

    public bool IsOpen => _overlay is not null;

    public void Open(
        Panel rootPanel,
        string titleText,
        string metaText,
        string bodyText,
        Action? removeAction,
        Action onClosed)
    {
        ArgumentNullException.ThrowIfNull(rootPanel);
        ArgumentNullException.ThrowIfNull(onClosed);

        Close();
        _rootPanel = rootPanel;
        _onClosed = onClosed;

        _overlay = new Panel();
        _overlay.Dock(Dock.Fill);
        rootPanel.AddChild(_overlay);

        var scrim = new Panel();
        scrim.Dock(Dock.Fill);
        _overlay.AddChild(scrim);
        GumUiLayout.AddSolidBackground(scrim, ContentUiColors.DetailScrim);

        var card = new Panel();
        GumUiLayout.CenterInParent(card, xPercent: 50f, yPercent: 50f);
        GumUiLayout.SetBoundedWidth(card, maxPixels: 480f, parentPercent: 90f);
        card.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _overlay.AddChild(card);
        GumUiLayout.AddSolidBackground(card, ContentUiColors.Panel);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 8f, widthPercent: 92f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        card.AddChild(stack);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var title = new Label { Text = titleText };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        var meta = new Label { Text = metaText };
        GumUiLayout.FillParentWidth(meta);
        stack.AddChild(meta);

        var body = new Label { Text = bodyText };
        GumUiLayout.FillParentWidth(body);
        stack.AddChild(body);

        var actionsHost = new Panel();
        GumUiLayout.FillParentWidth(actionsHost);
        actionsHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.AddChild(actionsHost);

        var detailButtons = new List<Button>();
        if (removeAction is not null)
        {
            var removeButton = new Button { Text = "Remove" };
            removeButton.Click += (_, _) => removeAction();
            detailButtons.Add(removeButton);
            _focusable.Add((removeButton, removeAction));
        }

        var closeButton = new Button { Text = "Close" };
        closeButton.Click += (_, _) => Close();
        detailButtons.Add(closeButton);
        _focusable.Add((closeButton, () => Close()));

        var canvasWidth = GumService.Default.CanvasWidth;
        var cardInner = Math.Min(480f, canvasWidth > 0 ? canvasWidth * 0.9f : 480f) * 0.92f;
        GumUiLayout.LayoutAdaptiveButtonRows(
            actionsHost,
            detailButtons,
            cardInner,
            spacing: 8f,
            minButtonWidth: 88f,
            preferredButtonWidth: 140f);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        _focusIndex = Math.Max(0, _focusable.Count - 1);
        ApplyFocusIndex();
    }

    public bool TryClose()
    {
        if (!IsOpen)
            return false;

        Close(notifyClosed: true);
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
        _focusIndex = 0;
        _rootPanel = null;

        var onClosed = _onClosed;
        _onClosed = null;
        if (notifyClosed)
            onClosed?.Invoke();
    }

    public void HandleInput(IGameCommandSource commands)
    {
        if (_focusable.Count == 0)
            return;

        if (commands.WasPressed(GameCommand.NavigateDown)
            || commands.WasPressed(GameCommand.NavigateRight))
        {
            _focusIndex = Math.Min(_focusIndex + 1, _focusable.Count - 1);
            ApplyFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateUp)
            || commands.WasPressed(GameCommand.NavigateLeft))
        {
            _focusIndex = Math.Max(_focusIndex - 1, 0);
            ApplyFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.Confirm))
        {
            _focusable[_focusIndex].Activate();
            return;
        }

        if (commands.WasPressed(GameCommand.Cancel)
            || commands.WasPressed(GameCommand.Back)
            || commands.WasPressed(GameCommand.Info)
            || commands.WasPressed(GameCommand.Pause))
        {
            Close();
            return;
        }

        MaintainFocus();
    }

    private void ApplyFocusIndex()
    {
        if (_focusable.Count == 0)
            return;

        _focusIndex = Math.Clamp(_focusIndex, 0, _focusable.Count - 1);
        foreach (var (button, _) in _focusable)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }

        _focusable[_focusIndex].Button.IsFocused = true;
    }

    private void MaintainFocus()
    {
        for (var index = 0; index < _focusable.Count; index++)
        {
            if (!_focusable[index].Button.IsFocused)
                continue;
            _focusIndex = index;
            return;
        }

        ApplyFocusIndex();
    }
}
