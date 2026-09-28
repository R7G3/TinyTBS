using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Content;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.LoadGame;

/// <summary>Load Game shell: save list, detail overlay, Back.</summary>
public sealed class LoadGameView
{
    private const float PanelMaxWidth = 640f;
    private const float ListMinHeight = 120f;
    private const float ListMaxHeight = 360f;
    private const float RowSpacing = 6f;
    private const float ListBottomPadding = GumScrollListLayout.DefaultListBottomPadding;

    private readonly LoadGameDetailOverlay _detail = new();

    private Panel? _rootPanel;
    private Panel? _shell;
    private Label? _statusLabel;
    private ScrollViewer? _listScroll;
    private Panel? _listPanel;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;
    private int _listFocusStartIndex = -1;
    private int _listFocusCount;
    private int _backFocusIndex = -1;

    public bool IsDetailOpen => _detail.IsOpen;

    public void Build(
        LoadGameViewModel viewModel,
        Action<LoadGameSaveRowViewModel> onOpenSave,
        Action onBack)
    {
        Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var bodyPanel = new Panel();
        bodyPanel.Dock(Dock.Fill);
        _rootPanel.AddChild(bodyPanel);

        _shell = new Panel();
        GumUiLayout.CenterInParent(_shell, xPercent: 50f, yPercent: 50f);
        GumUiLayout.SetBoundedWidth(_shell, PanelMaxWidth, parentPercent: 92f);
        _shell.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        bodyPanel.AddChild(_shell);
        GumUiLayout.AddSolidBackground(_shell, ContentUiColors.Panel);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 10f, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        _shell.AddChild(stack);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var title = new Label { Text = viewModel.Title };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        _statusLabel = new Label { Text = viewModel.StatusText };
        GumUiLayout.FillParentWidth(_statusLabel);
        stack.AddChild(_statusLabel);

        _listScroll = new ScrollViewer();
        GumUiLayout.FillParentWidth(_listScroll);
        _listScroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        _listScroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        _listScroll.InnerPanel.Width = 0;
        _listScroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        _listScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        GumScrollViewerChrome.DisableScrollChromeFocus(_listScroll);
        stack.AddChild(_listScroll);

        _listPanel = new Panel();
        _listPanel.Visual.HasEvents = false;
        _listPanel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _listPanel.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _listPanel.Visual.StackSpacing = RowSpacing;
        GumUiLayout.FillParentWidth(_listPanel);
        _listScroll.AddChild(_listPanel);

        _listFocusStartIndex = _focusableEntries.Count;
        if (viewModel.Saves.Count == 0)
        {
            var empty = new Label { Text = "No match saves yet. Use Pause → Save during a match." };
            GumUiLayout.FillParentWidth(empty);
            _listPanel.AddChild(empty);
            _listFocusCount = 0;
        }
        else
        {
            foreach (var save in viewModel.Saves)
            {
                var row = save;
                var button = new Button { Text = $"{row.Title}  —  {row.Meta}" };
                GumUiLayout.FillParentWidth(button);
                button.Click += (_, _) => onOpenSave(row);
                _listPanel.AddChild(button);
                _focusableEntries.Add((button, () => onOpenSave(row)));
            }

            _listFocusCount = viewModel.Saves.Count;
        }

        GumUiLayout.AddVerticalSpacer(_listPanel, ListBottomPadding);

        var contentHeight = GumScrollListLayout.MeasureStackContentHeight(_listPanel, RowSpacing) + 16f;
        var listHeight = Math.Min(ListMaxHeight, Math.Max(ListMinHeight, contentHeight));
        GumUiLayout.SetAbsoluteHeight(_listScroll, listHeight);

        var actionHost = GumMenuChrome.CreateButtonBarHost(stack);
        var actionButtons = new List<Button>();
        GumMenuChrome.AddActionButton(
            actionButtons,
            _focusableEntries,
            "Back",
            isEnabled: true,
            onActivate: onBack,
            onRegistered: index => _backFocusIndex = index);
        GumUiLayout.LayoutAdaptiveButtonRows(
            actionHost,
            actionButtons,
            availableWidth: PanelMaxWidth * 0.94f,
            spacing: 8f,
            minButtonWidth: 100f,
            preferredButtonWidth: 160f);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        _focusIndex = _listFocusCount > 0 ? _listFocusStartIndex : _backFocusIndex;
        if (_focusIndex < 0)
            _focusIndex = 0;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
    }

    public void SetStatus(string statusText)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = statusText;
    }

    public void OpenDetail(
        LoadGameSaveRowViewModel row,
        Action onLoad,
        Action onDelete)
    {
        if (_rootPanel is null)
            return;

        _detail.Open(
            _rootPanel,
            row.Title,
            row.Meta,
            onLoad,
            onDelete,
            onClosed: () =>
            {
                if (_focusableEntries.Count > 0)
                    GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
            });
    }

    public void CloseDetail() => _detail.Close();

    public bool TryCloseDetail() => _detail.TryClose();

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_detail.IsOpen)
        {
            _detail.HandleInput(commands, elapsedSeconds);
            return;
        }

        if (_focusableEntries.Count == 0)
            return;

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _focusableEntries,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds);
    }

    public void Clear()
    {
        _detail.Close();
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _shell = null;
        _statusLabel = null;
        _listScroll = null;
        _listPanel = null;
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;
        _listFocusStartIndex = -1;
        _listFocusCount = 0;
        _backFocusIndex = -1;
    }
}
