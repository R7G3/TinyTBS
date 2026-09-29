using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Content;

/// <summary>
/// Content library shell: panel, tabs, scroll host, bottom actions, focus navigation.
/// Tab list bodies and detail overlay live in sibling types.
/// </summary>
public sealed class ContentLibraryView
{
    private const float PanelMaxWidth = 720f;
    private const float ListMinHeight = 100f;
    private const float ListMaxHeight = 320f;
    private const float RowSpacing = 6f;
    private const float ListBottomPadding = GumScrollListLayout.DefaultListBottomPadding;
    private const float ChromeEstimateComfortable = 260f;
    private const float ChromeEstimateCompact = 220f;

    private readonly ContentLibraryDetailOverlay _detail = new();

    private Panel? _rootPanel;
    private Panel? _shell;
    private Label? _statusLabel;
    private Label? _hintsLabel;
    private ScrollViewer? _listScroll;
    private Panel? _listPanel;
    private Panel? _tabBarHost;
    private Panel? _actionBarHost;
    private ContentLibraryTab _activeTab;
    private Action<ContentLibraryTab>? _onSelectTab;
    private readonly List<Button> _tabButtons = [];
    private readonly List<Button> _actionButtons = [];
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;
    private int _ownedFocusIndex;
    private int _listFocusStartIndex = -1;
    private int _listFocusCount;
    private int _actionFocusStartIndex = -1;
    private int _actionFocusCount;
    private float _lastButtonBarWidth = -1f;
    private Action<string>? _onUninstallModule;
    private Action<string>? _onUninstallBundle;

    public bool IsDetailOpen => _detail.IsOpen;

    public void Build(
        ContentLibraryViewModel viewModel,
        Action<ContentLibraryTab> onSelectTab,
        Action<string> onUninstallModule,
        Action<string> onUninstallBundle,
        Action onPickInstallFromDevice,
        Action<string> onInstallArchive,
        Action onBack)
    {
        Clear();

        _activeTab = viewModel.ActiveTab;
        _onSelectTab = onSelectTab;
        _onUninstallModule = onUninstallModule;
        _onUninstallBundle = onUninstallBundle;

        var canvasHeight = GumService.Default.CanvasHeight;
        var compact = canvasHeight > 0f && canvasHeight < 640f;
        var spacer = compact ? 8f : 16f;
        var stackSpacing = compact ? 6f : 10f;

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
        GumUiLayout.AddSolidBackground(_shell, UiColors.MenuPanel);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: stackSpacing, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        _shell.AddChild(stack);

        GumUiLayout.AddVerticalSpacer(stack, spacer);

        var title = new Label { Text = viewModel.Title };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        _statusLabel = new Label { Text = viewModel.StatusText };
        GumUiLayout.FillParentWidth(_statusLabel);
        stack.AddChild(_statusLabel);

        AddTabBar(stack, viewModel.ActiveTab);

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

        _listFocusStartIndex = _focusableEntries.Count;
        var listBody = new ContentLibraryListBuilder(_listPanel, _focusableEntries);
        PopulateActiveTab(listBody, viewModel, onPickInstallFromDevice, onInstallArchive);
        GumUiLayout.AddVerticalSpacer(_listPanel, ListBottomPadding);
        _listFocusCount = _focusableEntries.Count - _listFocusStartIndex;

        AddBottomActions(stack, viewModel, onBack);

        _hintsLabel = new Label { Text = BuildHintsText(compact) };
        GumUiLayout.FillParentWidth(_hintsLabel);
        stack.AddChild(_hintsLabel);

        GumUiLayout.AddVerticalSpacer(stack, spacer);

        ApplyResponsiveLayout();
        FocusInitial();
    }

    /// <summary>Recompute list height and button rows when the Gum canvas size changes.</summary>
    public void ApplyResponsiveLayout()
    {
        if (_listScroll is null || _shell is null)
            return;

        var canvasWidth = GumService.Default.CanvasWidth;
        var canvasHeight = GumService.Default.CanvasHeight;
        if (canvasWidth <= 0f || canvasHeight <= 0f)
            return;

        GumUiLayout.SetBoundedWidth(_shell, PanelMaxWidth, parentPercent: 92f);

        var panelInnerWidth = Math.Min(PanelMaxWidth, canvasWidth * 0.92f) * 0.94f;
        var chrome = canvasHeight < 640f || canvasWidth < 480f
            ? ChromeEstimateCompact + 40f
            : ChromeEstimateComfortable;
        var heightBudget = Math.Max(ListMinHeight, canvasHeight * 0.94f - chrome);
        var contentHeight = _listPanel is null
            ? ListMinHeight
            : GumScrollListLayout.MeasureStackContentHeight(_listPanel, RowSpacing) + 16f;
        var listHeight = Math.Min(heightBudget, Math.Min(ListMaxHeight, Math.Max(ListMinHeight, contentHeight)));

        GumUiLayout.SetAbsoluteHeight(_listScroll, listHeight);
        _shell.Visual.MaxHeight = canvasHeight * 0.94f;

        if (Math.Abs(panelInnerWidth - _lastButtonBarWidth) > 0.5f)
        {
            _lastButtonBarWidth = panelInnerWidth;

            if (_tabBarHost is not null && _tabButtons.Count > 0)
            {
                GumUiLayout.LayoutAdaptiveButtonRows(
                    _tabBarHost,
                    _tabButtons,
                    panelInnerWidth,
                    spacing: 8f,
                    minButtonWidth: 88f,
                    preferredButtonWidth: 120f);
            }

            if (_actionBarHost is not null && _actionButtons.Count > 0)
            {
                GumUiLayout.LayoutAdaptiveButtonRows(
                    _actionBarHost,
                    _actionButtons,
                    panelInnerWidth,
                    spacing: 8f,
                    minButtonWidth: 72f,
                    preferredButtonWidth: 110f);
            }
        }

        if (_hintsLabel is not null)
            _hintsLabel.Text = BuildHintsText(canvasHeight < 640f || canvasWidth < 520f);

        ApplyListWellBackground(_listScroll);
        GumScrollViewerChrome.DisableScrollChromeFocus(_listScroll);
    }

    private static string BuildHintsText(bool compact) =>
        compact
            ? "Up/Down  ·  Confirm  ·  L/R tabs or actions  ·  Esc/B"
            : "Up/Down list  ·  Confirm  ·  Left/Right: tabs (or bottom buttons)  ·  Esc / B back";

    public void SetStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text;
    }

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (IsDetailOpen)
        {
            _detail.HandleInput(commands, elapsedSeconds);
            return;
        }

        if (_focusableEntries.Count == 0)
            return;

        if (commands.WasPressed(GameCommand.NavigateLeft))
        {
            _navigateRepeat.Reset();
            HandleHorizontalNavigate(-1);
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateRight))
        {
            _navigateRepeat.Reset();
            HandleHorizontalNavigate(+1);
            return;
        }

        var verticalDelta = _navigateRepeat.TryGetDelta(commands, elapsedSeconds);
        if (verticalDelta != 0)
        {
            RememberOwnedFocusFromUi();
            _focusIndex = Math.Clamp(_focusIndex + verticalDelta, 0, _focusableEntries.Count - 1);
            ApplyFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.Confirm))
        {
            RememberOwnedFocusFromUi();
            _focusableEntries[_focusIndex].Activate();
            return;
        }

        MaintainFocus();
    }

    /// <summary>Closes the module detail popup if open.</summary>
    public bool TryCloseDetail() => _detail.TryClose();

    public void Clear()
    {
        _detail.Close(notifyClosed: false);
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _shell = null;
        _statusLabel = null;
        _hintsLabel = null;
        _listScroll = null;
        _listPanel = null;
        _tabBarHost = null;
        _actionBarHost = null;
        _onSelectTab = null;
        _tabButtons.Clear();
        _actionButtons.Clear();
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _onUninstallModule = null;
        _onUninstallBundle = null;
        _focusIndex = 0;
        _ownedFocusIndex = 0;
        _listFocusStartIndex = -1;
        _listFocusCount = 0;
        _actionFocusStartIndex = -1;
        _actionFocusCount = 0;
        _lastButtonBarWidth = -1f;
    }

    private void PopulateActiveTab(
        ContentLibraryListBuilder list,
        ContentLibraryViewModel viewModel,
        Action onPickInstallFromDevice,
        Action<string> onInstallArchive)
    {
        switch (viewModel.ActiveTab)
        {
            case ContentLibraryTab.Bundles:
                ContentLibraryBundlesTabBody.Populate(list, viewModel.Bundles, ShowBundleDetail);
                break;
            case ContentLibraryTab.Install:
                ContentLibraryInstallTabBody.Populate(
                    list,
                    viewModel,
                    onPickInstallFromDevice,
                    onInstallArchive);
                break;
            default:
                ContentLibraryModulesTabBody.Populate(list, viewModel.Modules, ShowModuleDetail);
                break;
        }
    }

    private void ShowModuleDetail(ContentModuleRowViewModel module)
    {
        if (_rootPanel is null)
            return;

        _detail.Open(
            _rootPanel,
            module.Title,
            $"{module.TypeLabel}  ·  v{module.Version}  ·  {module.SourceLabel}  ·  {module.ModuleId}",
            string.IsNullOrWhiteSpace(module.Description) ? "No description." : module.Description,
            module.CanUninstall
                ? () =>
                {
                    var moduleId = module.ModuleId;
                    _detail.Close();
                    _onUninstallModule?.Invoke(moduleId);
                }
                : null,
            onClosed: RestoreShellFocusAfterDetail);
    }

    private void ShowBundleDetail(ContentBundleRowViewModel bundle)
    {
        if (_rootPanel is null)
            return;

        _detail.Open(
            _rootPanel,
            bundle.Title,
            $"bundle  ·  {bundle.SourceLabel}  ·  {bundle.BundleId}",
            "Modules: " + bundle.ModulesSummary,
            bundle.CanUninstall
                ? () =>
                {
                    var bundleId = bundle.BundleId;
                    _detail.Close();
                    _onUninstallBundle?.Invoke(bundleId);
                }
                : null,
            onClosed: RestoreShellFocusAfterDetail);
    }

    private void RestoreShellFocusAfterDetail()
    {
        if (_focusableEntries.Count > 0)
            ApplyFocusIndex();
    }

    private void HandleHorizontalNavigate(int delta)
    {
        if (_focusableEntries.Count == 0)
            return;

        _focusIndex = Math.Clamp(_ownedFocusIndex, 0, _focusableEntries.Count - 1);

        if (GumMenuChrome.IsFocusIndexInRange(_focusIndex, _actionFocusStartIndex, _actionFocusCount))
        {
            GumMenuChrome.MoveFocusInRange(ref _focusIndex, _actionFocusStartIndex, _actionFocusCount, delta);
            ApplyFocusIndex();
            return;
        }

        TrySwitchTab(delta);
        ApplyFocusIndex();
    }

    private void RememberOwnedFocusFromUi()
    {
        if (GumFocusableButtonList.SyncFocusIndexFromUi(_focusableEntries, ref _focusIndex))
            _ownedFocusIndex = _focusIndex;
    }

    private void TrySwitchTab(int delta)
    {
        if (_onSelectTab is null)
            return;

        var ordered = new[]
        {
            ContentLibraryTab.Modules,
            ContentLibraryTab.Bundles,
            ContentLibraryTab.Install,
        };
        var currentIndex = Array.IndexOf(ordered, _activeTab);
        if (currentIndex < 0)
            return;

        var nextIndex = Math.Clamp(currentIndex + delta, 0, ordered.Length - 1);
        var next = ordered[nextIndex];
        if (next != _activeTab)
            _onSelectTab(next);
    }

    private void AddTabBar(Panel parent, ContentLibraryTab activeTab)
    {
        _tabBarHost = GumMenuChrome.CreateButtonBarHost(parent);

        GumMenuChrome.AddTabButton(
            _tabButtons,
            _focusableEntries,
            "Modules",
            activeTab == ContentLibraryTab.Modules,
            () => _onSelectTab?.Invoke(ContentLibraryTab.Modules));
        GumMenuChrome.AddTabButton(
            _tabButtons,
            _focusableEntries,
            "Bundles",
            activeTab == ContentLibraryTab.Bundles,
            () => _onSelectTab?.Invoke(ContentLibraryTab.Bundles));
        GumMenuChrome.AddTabButton(
            _tabButtons,
            _focusableEntries,
            "Install",
            activeTab == ContentLibraryTab.Install,
            () => _onSelectTab?.Invoke(ContentLibraryTab.Install));
    }

    private void AddBottomActions(Panel parent, ContentLibraryViewModel viewModel, Action onBack)
    {
        _actionBarHost = GumMenuChrome.CreateButtonBarHost(parent);

        _actionFocusStartIndex = _focusableEntries.Count;

        // Install is also a top tab; this button is a shortcut to the same place.
        GumMenuChrome.AddActionButton(
            _actionButtons,
            _focusableEntries,
            "Install",
            isEnabled: true,
            () => _onSelectTab?.Invoke(ContentLibraryTab.Install));
        GumMenuChrome.AddActionButton(_actionButtons, _focusableEntries, "Download", viewModel.CanDownload, () => { });
        GumMenuChrome.AddActionButton(_actionButtons, _focusableEntries, "Update", viewModel.CanUpdate, () => { });
        GumMenuChrome.AddActionButton(_actionButtons, _focusableEntries, "Back", isEnabled: true, onBack);

        _actionFocusCount = _focusableEntries.Count - _actionFocusStartIndex;
    }

    private void FocusInitial()
    {
        // After opening Install via the bottom button, land on the Install tab control
        // so the jump to "first list row" is not confusing.
        if (_activeTab == ContentLibraryTab.Install && _tabButtons.Count >= 3)
        {
            _focusIndex = 2;
            ApplyFocusIndex();
            return;
        }

        if (_listFocusCount > 0 && _listFocusStartIndex >= 0)
            _focusIndex = _listFocusStartIndex;
        else
            _focusIndex = 0;

        ApplyFocusIndex();
    }

    private void MaintainFocus()
    {
        StealFocusFromScrollChrome();
        if (GumFocusableButtonList.SyncFocusIndexFromUi(_focusableEntries, ref _focusIndex)
            && GumFocusableButtonList.IsFocusIntact(_focusableEntries, _focusIndex))
        {
            // Do not EnsureFocusedRowVisible every frame — that fights mouse-wheel scrolling.
            _ownedFocusIndex = _focusIndex;
            return;
        }

        ApplyFocusIndex();
    }

    private void ApplyFocusIndex()
    {
        if (_focusableEntries.Count == 0)
            return;

        StealFocusFromScrollChrome();
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
        _ownedFocusIndex = _focusIndex;
        EnsureFocusedRowVisible();
    }

    private void EnsureFocusedRowVisible()
    {
        if (_listScroll is null
            || _listPanel is null
            || _listFocusStartIndex < 0
            || _focusIndex < _listFocusStartIndex
            || _focusIndex >= _listFocusStartIndex + _listFocusCount)
        {
            return;
        }

        GumScrollListLayout.EnsureFocusedRowVisible(
            _listScroll,
            _listPanel,
            _focusableEntries[_focusIndex].Button.Visual,
            _listFocusStartIndex,
            _listFocusCount,
            _focusIndex,
            RowSpacing,
            ListMinHeight,
            listBottomPadding: ListBottomPadding);
    }

    private void StealFocusFromScrollChrome() =>
        GumScrollViewerChrome.StealFocusFromScrollChrome(_listScroll);

    private static void ApplyListWellBackground(ScrollViewer scrollViewer)
    {
        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return;

        visual.BackgroundColor = UiColors.MenuListWell;
    }
}
