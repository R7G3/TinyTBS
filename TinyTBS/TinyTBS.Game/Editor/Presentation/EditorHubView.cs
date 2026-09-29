using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Editor.ViewModels;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Editor hub: module list, New Scenario, Open/Duplicate, greyed Publish, Back.</summary>
public sealed class EditorHubView
{
    private const float PanelMaxWidth = 720f;
    private const float ListMinHeight = 100f;
    private const float ListMaxHeight = 320f;
    private const float RowSpacing = 6f;
    private const float ListBottomPadding = GumScrollListLayout.DefaultListBottomPadding;

    private Panel? _rootPanel;
    private Panel? _shell;
    private Label? _statusLabel;
    private Label? _openLabel;
    private ScrollViewer? _listScroll;
    private Panel? _listPanel;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private readonly EditorMapDetailOverlay _mapDetail = new();
    private int _focusIndex;

    public bool IsMapDetailOpen => _mapDetail.IsOpen;

    public void Build(
        EditorHubViewModel viewModel,
        Action onNewScenario,
        Action onNewMap,
        Action<EditorModuleRowViewModel> onActivateModule,
        Action<string> onOpenMap,
        Action<string> onDeleteMap,
        Action onCloseModule,
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
        GumUiLayout.AddSolidBackground(_shell, EditorUiColors.Panel);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 10f, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        _shell.AddChild(stack);

        GumUiLayout.AddVerticalSpacer(stack, 12f);

        var title = new Label { Text = viewModel.Title };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        _statusLabel = new Label { Text = viewModel.StatusText };
        GumUiLayout.FillParentWidth(_statusLabel);
        stack.AddChild(_statusLabel);

        _openLabel = new Label
        {
            Text = viewModel.HasOpenModule
                ? $"Open: {viewModel.OpenModuleId} — {viewModel.OpenModuleTitle}"
                : "Open: (none)",
        };
        GumUiLayout.FillParentWidth(_openLabel);
        stack.AddChild(_openLabel);

        _listScroll = new ScrollViewer();
        GumUiLayout.FillParentWidth(_listScroll);
        _listScroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        _listScroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        _listScroll.InnerPanel.Width = 0;
        _listScroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        _listScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ApplyListWellBackground(_listScroll);
        GumScrollViewerChrome.DisableScrollChromeFocus(_listScroll);
        stack.AddChild(_listScroll);

        _listPanel = new Panel();
        _listPanel.Visual.HasEvents = false;
        _listPanel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _listPanel.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _listPanel.Visual.StackSpacing = RowSpacing;
        GumUiLayout.FillParentWidth(_listPanel);
        _listScroll.AddChild(_listPanel);

        if (viewModel.HasOpenModule && viewModel.Maps.Count > 0)
        {
            var mapsHeader = new Label { Text = "Maps in open module:" };
            GumUiLayout.FillParentWidth(mapsHeader);
            _listPanel.AddChild(mapsHeader);
            foreach (var mapId in viewModel.Maps)
            {
                var captured = mapId;
                AddFocusableButton(
                    _listPanel,
                    "Map: " + captured,
                    () => ShowMapDetail(captured, onOpenMap, onDeleteMap));
            }
        }

        if (viewModel.Modules.Count == 0)
        {
            var hint = new Label { Text = "No modules found. Create a scenario or duplicate bundled." };
            GumUiLayout.FillParentWidth(hint);
            _listPanel.AddChild(hint);
        }
        else
        {
            var modulesHeader = new Label { Text = "Modules:" };
            GumUiLayout.FillParentWidth(modulesHeader);
            _listPanel.AddChild(modulesHeader);
            foreach (var module in viewModel.Modules)
            {
                var captured = module;
                AddFocusableButton(
                    _listPanel,
                    captured.SummaryLine,
                    () => onActivateModule(captured));
            }
        }

        GumUiLayout.AddVerticalSpacer(_listPanel, ListBottomPadding);

        var actionBar = GumUiLayout.CreateVerticalStackPanel(spacing: 6f, widthPercent: 100f);
        stack.AddChild(actionBar);

        AddFocusableButton(actionBar, "New Scenario Module", onNewScenario);
        AddFocusableButton(
            actionBar,
            "New Map",
            onNewMap,
            isEnabled: viewModel.CanCreateMap);
        AddFocusableButton(
            actionBar,
            "Close Module",
            onCloseModule,
            isEnabled: viewModel.HasOpenModule);
        AddFocusableButton(actionBar, "Publish", () => { }, isEnabled: viewModel.CanPublish);
        AddFocusableButton(actionBar, "Back", onBack);

        var hints = new Label
        {
            Text = "Open a user scenario, then New Map. Confirm map row for Open/Delete.",
        };
        GumUiLayout.FillParentWidth(hints);
        stack.AddChild(hints);

        GumUiLayout.AddVerticalSpacer(stack, 12f);

        ApplyListHeight();
        FocusFirst();
    }

    public void SyncStatus(EditorHubViewModel viewModel)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = viewModel.StatusText ?? string.Empty;

        if (_openLabel is not null)
        {
            _openLabel.Text = viewModel.HasOpenModule
                ? $"Open: {viewModel.OpenModuleId} — {viewModel.OpenModuleTitle}"
                : "Open: (none)";
        }
    }

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_mapDetail.IsOpen)
        {
            _mapDetail.HandleInput(commands, elapsedSeconds);
            return;
        }

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _focusableEntries,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds);
        GumScrollViewerChrome.StealFocusFromScrollChrome(_listScroll);
    }

    public bool TryCloseMapDetail() => _mapDetail.TryClose();

    public void ApplyResponsiveLayout() => ApplyListHeight();

    public void FocusFirst()
    {
        if (_mapDetail.IsOpen)
            return;
        _focusIndex = 0;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
    }

    public void Clear()
    {
        _mapDetail.Close(notifyClosed: false);
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _shell = null;
        _statusLabel = null;
        _openLabel = null;
        _listScroll = null;
        _listPanel = null;
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;
    }

    private void ShowMapDetail(string mapId, Action<string> onOpenMap, Action<string> onDeleteMap)
    {
        if (_rootPanel is null)
            return;

        _mapDetail.Open(
            _rootPanel,
            mapId,
            onOpen: () =>
            {
                _mapDetail.Close(notifyClosed: false);
                onOpenMap(mapId);
            },
            onDelete: () =>
            {
                _mapDetail.Close(notifyClosed: false);
                onDeleteMap(mapId);
            },
            onClosed: () =>
            {
                if (_focusableEntries.Count > 0)
                    GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
            });
    }

    private void ApplyListHeight()
    {
        if (_listScroll is null)
            return;

        var canvasHeight = GumService.Default.CanvasHeight;
        var height = canvasHeight > 0f && canvasHeight < 640f
            ? Math.Max(ListMinHeight, ListMaxHeight * 0.7f)
            : ListMaxHeight;
        _listScroll.Visual.Height = height;
    }

    private void AddFocusableButton(Panel parent, string text, Action onClick, bool isEnabled = true)
    {
        var button = new Button { Text = text, IsEnabled = isEnabled };
        GumUiLayout.FillParentWidth(button);
        if (isEnabled)
        {
            button.Click += (_, _) => onClick();
            _focusableEntries.Add((button, onClick));
        }
        else
        {
            button.Visual.HasEvents = false;
        }

        parent.AddChild(button);
    }

    private static void ApplyListWellBackground(ScrollViewer scrollViewer)
    {
        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return;

        visual.BackgroundColor = EditorUiColors.ListWell;
    }
}
