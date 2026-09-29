using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Content;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Generic Open / Delete / Back detail for hub library rows (unit/building types).</summary>
public sealed class EditorItemDetailOverlay
{
    private Panel? _rootPanel;
    private Panel? _overlay;
    private readonly List<(Button Button, Action Activate)> _focusable = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;
    private Action? _onClosed;

    public bool IsOpen => _overlay is not null;

    public void Open(
        Panel rootPanel,
        string title,
        string metaLine,
        Action onOpen,
        Action onDelete,
        Action onClosed)
    {
        ArgumentNullException.ThrowIfNull(rootPanel);
        Close(notifyClosed: false);
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
        GumUiLayout.AddSolidBackground(card, EditorUiColors.Panel);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 8f, widthPercent: 92f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        card.AddChild(stack);
        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var titleLabel = new Label { Text = title };
        GumUiLayout.FillParentWidth(titleLabel);
        stack.AddChild(titleLabel);

        var meta = new Label { Text = metaLine };
        GumUiLayout.FillParentWidth(meta);
        stack.AddChild(meta);

        var actionsHost = new Panel();
        GumUiLayout.FillParentWidth(actionsHost);
        actionsHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.AddChild(actionsHost);

        var detailButtons = new List<Button>();
        var openButton = new Button { Text = "Open" };
        openButton.Click += (_, _) => onOpen();
        detailButtons.Add(openButton);
        _focusable.Add((openButton, onOpen));

        var deleteButton = new Button { Text = "Delete" };
        deleteButton.Click += (_, _) => onDelete();
        detailButtons.Add(deleteButton);
        _focusable.Add((deleteButton, onDelete));

        var backButton = new Button { Text = "Back" };
        backButton.Click += (_, _) => Close();
        detailButtons.Add(backButton);
        _focusable.Add((backButton, () => Close()));

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

        _focusIndex = 0;
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

        GumFocusableButtonList.HandleHorizontalInput(
            commands,
            _focusable,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds);
    }
}
