using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Content;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Presentation.LoadGame;

/// <summary>Detail card over Load Game: Load / Delete / Close.</summary>
internal sealed class LoadGameDetailOverlay
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
        string titleText,
        string metaText,
        Action onLoad,
        Action onDelete,
        Action onClosed)
    {
        ArgumentNullException.ThrowIfNull(rootPanel);
        ArgumentNullException.ThrowIfNull(onLoad);
        ArgumentNullException.ThrowIfNull(onDelete);
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

        var hint = new Label { Text = "Load resumes this match. Delete removes the file." };
        GumUiLayout.FillParentWidth(hint);
        stack.AddChild(hint);

        var actionsHost = new Panel();
        GumUiLayout.FillParentWidth(actionsHost);
        actionsHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.AddChild(actionsHost);

        var detailButtons = new List<Button>();

        var loadButton = new Button { Text = "Load" };
        loadButton.Click += (_, _) => onLoad();
        detailButtons.Add(loadButton);
        _focusable.Add((loadButton, onLoad));

        var deleteButton = new Button { Text = "Delete" };
        deleteButton.Click += (_, _) => onDelete();
        detailButtons.Add(deleteButton);
        _focusable.Add((deleteButton, onDelete));

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

        _focusIndex = 0;
        GumFocusableButtonList.ApplyFocus(_focusable, ref _focusIndex);
    }

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_overlay is null || _focusable.Count == 0)
            return;

        GumFocusableButtonList.HandleHorizontalInput(
            commands,
            _focusable,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds);
    }

    public bool TryClose()
    {
        if (_overlay is null)
            return false;
        Close();
        return true;
    }

    public void Close()
    {
        if (_overlay is null)
        {
            _focusable.Clear();
            _navigateRepeat.Reset();
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
                    _rootPanel.Visual.Children.RemoveAt(i);
            }
        }

        _overlay = null;
        _rootPanel = null;
        _focusable.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;

        var closed = _onClosed;
        _onClosed = null;
        closed?.Invoke();
    }
}
