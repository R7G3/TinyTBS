using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Content;

/// <summary>
/// Polished content library: panel, Modules/Bundles tabs, scrollable rows, bottom actions.
/// Keyboard / gamepad / mouse via focus list + <see cref="GameCommand"/>.
/// </summary>
public sealed class ContentLibraryView
{
    private const float PanelMaxWidth = 720f;
    private const float ListMinHeight = 100f;
    private const float ListMaxHeight = 320f;
    private const float RowSpacing = 6f;
    private const float RowPitchEstimate = 48f;
    private const float ChromeEstimateComfortable = 260f;
    private const float ChromeEstimateCompact = 220f;

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
    private int _focusIndex;
    private int _ownedFocusIndex;
    private int _listFocusStartIndex = -1;
    private int _listFocusCount;
    private int _listRowCount;
    private float _listRowPitch = RowPitchEstimate;
    private int _actionFocusStartIndex = -1;
    private int _actionFocusCount;
    private float _lastButtonBarWidth = -1f;
    private Action<string>? _onUninstallModule;
    private Panel? _detailOverlay;
    private readonly List<(Button Button, Action Activate)> _detailFocusable = [];
    private int _detailFocusIndex;

    public bool IsDetailOpen => _detailOverlay is not null;

    public void Build(
        ContentLibraryViewModel viewModel,
        Action<ContentLibraryTab> onSelectTab,
        Action<string> onUninstallModule,
        Action onPickInstallFromDevice,
        Action<string> onInstallArchive,
        Action onBack)
    {
        Clear();

        _activeTab = viewModel.ActiveTab;
        _onSelectTab = onSelectTab;
        _onUninstallModule = onUninstallModule;

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
        GumUiLayout.AddSolidBackground(_shell, ContentUiColors.Panel);

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
        DisableScrollChromeFocus(_listScroll);
        stack.AddChild(_listScroll);

        _listPanel = new Panel();
        _listPanel.Visual.HasEvents = false;
        _listPanel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _listPanel.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _listPanel.Visual.StackSpacing = RowSpacing;
        GumUiLayout.FillParentWidth(_listPanel);
        _listScroll.AddChild(_listPanel);

        _listFocusStartIndex = _focusableEntries.Count;
        PopulateList(viewModel, onPickInstallFromDevice, onInstallArchive);
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
        var contentHeight = Math.Max(_listRowCount, 1) * _listRowPitch + 16f;
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
        DisableScrollChromeFocus(_listScroll);
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

    public void HandleInput(IGameCommandSource commands)
    {
        if (IsDetailOpen)
        {
            HandleDetailInput(commands);
            return;
        }

        if (_focusableEntries.Count == 0)
            return;

        if (commands.WasPressed(GameCommand.NavigateLeft))
        {
            HandleHorizontalNavigate(-1);
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateRight))
        {
            HandleHorizontalNavigate(+1);
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateDown))
        {
            RememberOwnedFocusFromUi();
            _focusIndex = Math.Min(_focusIndex + 1, _focusableEntries.Count - 1);
            ApplyFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateUp))
        {
            RememberOwnedFocusFromUi();
            _focusIndex = Math.Max(_focusIndex - 1, 0);
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
    public bool TryCloseDetail()
    {
        if (!IsDetailOpen)
            return false;

        CloseDetail();
        return true;
    }

    public void Clear()
    {
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
        _onUninstallModule = null;
        CloseDetail();
        _focusIndex = 0;
        _ownedFocusIndex = 0;
        _listFocusStartIndex = -1;
        _listFocusCount = 0;
        _listRowCount = 0;
        _listRowPitch = RowPitchEstimate;
        _actionFocusStartIndex = -1;
        _actionFocusCount = 0;
        _lastButtonBarWidth = -1f;
    }

    /// <summary>
    /// Left/Right: bottom actions move focus along that row; tabs/list switch tabs.
    /// Uses owned focus only — after Gum Update, Left/Right may already have moved
    /// highlight to a neighbor (often Back); do not follow that for region detection.
    /// </summary>
    private void HandleHorizontalNavigate(int delta)
    {
        if (_focusableEntries.Count == 0)
            return;

        _focusIndex = Math.Clamp(_ownedFocusIndex, 0, _focusableEntries.Count - 1);

        if (IsFocusIndexOnActionBar())
        {
            MoveActionFocus(delta);
            return;
        }

        TrySwitchTab(delta);
        ApplyFocusIndex();
    }

    private bool IsFocusIndexOnActionBar() =>
        _actionFocusCount > 0
        && _actionFocusStartIndex >= 0
        && _focusIndex >= _actionFocusStartIndex
        && _focusIndex < _actionFocusStartIndex + _actionFocusCount;

    private void RememberOwnedFocusFromUi()
    {
        SyncFocusIndexFromUi();
        if (_focusIndex >= 0 && _focusIndex < _focusableEntries.Count)
            _ownedFocusIndex = _focusIndex;
    }

    private void MoveActionFocus(int delta)
    {
        if (_actionFocusCount <= 0 || _actionFocusStartIndex < 0)
            return;

        var localIndex = _focusIndex - _actionFocusStartIndex;
        localIndex = Math.Clamp(localIndex + delta, 0, _actionFocusCount - 1);
        _focusIndex = _actionFocusStartIndex + localIndex;
        ApplyFocusIndex();
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
        _tabBarHost = new Panel();
        GumUiLayout.FillParentWidth(_tabBarHost);
        _tabBarHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.AddChild(_tabBarHost);

        AddTabButton(
            "Modules",
            activeTab == ContentLibraryTab.Modules,
            () => _onSelectTab?.Invoke(ContentLibraryTab.Modules));
        AddTabButton(
            "Bundles",
            activeTab == ContentLibraryTab.Bundles,
            () => _onSelectTab?.Invoke(ContentLibraryTab.Bundles));
        AddTabButton(
            "Install",
            activeTab == ContentLibraryTab.Install,
            () => _onSelectTab?.Invoke(ContentLibraryTab.Install));
    }

    private void AddTabButton(string text, bool isActive, Action onActivate)
    {
        var button = new Button
        {
            Text = isActive ? $"[{text}]" : text,
            IsEnabled = true,
        };
        button.Click += (_, _) => onActivate();
        _tabButtons.Add(button);
        _focusableEntries.Add((button, onActivate));
    }

    private void PopulateList(
        ContentLibraryViewModel viewModel,
        Action onPickInstallFromDevice,
        Action<string> onInstallArchive)
    {
        ArgumentNullException.ThrowIfNull(_listPanel);

        switch (viewModel.ActiveTab)
        {
            case ContentLibraryTab.Bundles:
                PopulateBundles(viewModel.Bundles);
                _listRowCount = viewModel.Bundles.Count;
                _listRowPitch = 72f;
                break;
            case ContentLibraryTab.Install:
                PopulateInstalls(viewModel, onPickInstallFromDevice, onInstallArchive);
                break;
            default:
                PopulateModules(viewModel.Modules);
                _listRowCount = viewModel.Modules.Count;
                _listRowPitch = RowPitchEstimate;
                break;
        }
    }

    private void PopulateModules(IReadOnlyList<ContentModuleRowViewModel> modules)
    {
        if (modules.Count == 0)
        {
            AddEmptyHint("No modules in the library.");
            return;
        }

        foreach (var module in modules)
        {
            var captured = module;
            AddListRow(captured.SummaryLine, () => ShowModuleDetail(captured));
        }
    }

    private void PopulateBundles(IReadOnlyList<ContentBundleRowViewModel> bundles)
    {
        if (bundles.Count == 0)
        {
            AddEmptyHint("No bundle presets.");
            return;
        }

        foreach (var bundle in bundles)
        {
            var captured = bundle;
            AddTwoLineListRow(
                captured.SummaryLine,
                captured.DetailLine,
                () => ShowBundleDetail(captured));
        }
    }

    private void PopulateInstalls(
        ContentLibraryViewModel viewModel,
        Action onPickInstallFromDevice,
        Action<string> onInstallArchive)
    {
        _listRowPitch = RowPitchEstimate;

        AddListRow("From device…", onPickInstallFromDevice);
        AddListRow(
            "From catalog… (soon)",
            onActivate: () => { },
            isEnabled: viewModel.CanInstallFromCatalog);

        var pending = viewModel.PendingInstalls;
        if (pending.Count > 0)
        {
            AddEmptyHint("Queued in app Downloads (confirm to install):");
            foreach (var item in pending)
            {
                var path = item.FullPath;
                AddListRow(item.FileName, () => onInstallArchive(path));
            }

            _listRowCount = 2 + 1 + pending.Count;
        }
        else
        {
            AddEmptyHint(
                "Pick a .tinymod.zip from your device, or use the catalog when it is available.");
            _listRowCount = 3;
        }
    }

    private void ShowModuleDetail(ContentModuleRowViewModel module)
    {
        if (_rootPanel is null)
            return;

        CloseDetail();
        OpenDetailCard(
            module.Title,
            $"{module.TypeLabel}  ·  v{module.Version}  ·  {module.SourceLabel}  ·  {module.ModuleId}",
            string.IsNullOrWhiteSpace(module.Description) ? "No description." : module.Description,
            module.CanUninstall
                ? () =>
                {
                    var moduleId = module.ModuleId;
                    CloseDetail();
                    _onUninstallModule?.Invoke(moduleId);
                }
                : null);
    }

    private void ShowBundleDetail(ContentBundleRowViewModel bundle)
    {
        if (_rootPanel is null)
            return;

        CloseDetail();
        OpenDetailCard(
            bundle.Title,
            $"bundle  ·  {bundle.SourceLabel}  ·  {bundle.BundleId}",
            "Modules: " + bundle.ModulesSummary,
            removeAction: null);
    }

    private void OpenDetailCard(string titleText, string metaText, string bodyText, Action? removeAction)
    {
        ArgumentNullException.ThrowIfNull(_rootPanel);

        _detailOverlay = new Panel();
        _detailOverlay.Dock(Dock.Fill);
        _rootPanel.AddChild(_detailOverlay);

        var scrim = new Panel();
        scrim.Dock(Dock.Fill);
        _detailOverlay.AddChild(scrim);
        GumUiLayout.AddSolidBackground(scrim, ContentUiColors.DetailScrim);

        var card = new Panel();
        GumUiLayout.CenterInParent(card, xPercent: 50f, yPercent: 50f);
        GumUiLayout.SetBoundedWidth(card, maxPixels: 480f, parentPercent: 90f);
        card.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _detailOverlay.AddChild(card);
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
            _detailFocusable.Add((removeButton, removeAction));
        }

        var closeButton = new Button { Text = "Close" };
        closeButton.Click += (_, _) => CloseDetail();
        detailButtons.Add(closeButton);
        _detailFocusable.Add((closeButton, CloseDetail));

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

        _detailFocusIndex = Math.Max(0, _detailFocusable.Count - 1);
        ApplyDetailFocusIndex();
    }

    private void CloseDetail()
    {
        if (_detailOverlay is not null)
        {
            if (_rootPanel is not null)
            {
                for (var i = _rootPanel.Visual.Children.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(_rootPanel.Visual.Children[i], _detailOverlay.Visual))
                    {
                        _rootPanel.Visual.Children.RemoveAt(i);
                        break;
                    }
                }
            }

            _detailOverlay.Visual.Parent = null;
            _detailOverlay.IsVisible = false;
            _detailOverlay = null;
        }

        _detailFocusable.Clear();
        _detailFocusIndex = 0;
        if (_focusableEntries.Count > 0)
            ApplyFocusIndex();
    }

    private void HandleDetailInput(IGameCommandSource commands)
    {
        if (_detailFocusable.Count == 0)
            return;

        if (commands.WasPressed(GameCommand.NavigateDown)
            || commands.WasPressed(GameCommand.NavigateRight))
        {
            _detailFocusIndex = Math.Min(_detailFocusIndex + 1, _detailFocusable.Count - 1);
            ApplyDetailFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateUp)
            || commands.WasPressed(GameCommand.NavigateLeft))
        {
            _detailFocusIndex = Math.Max(_detailFocusIndex - 1, 0);
            ApplyDetailFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.Confirm))
        {
            _detailFocusable[_detailFocusIndex].Activate();
            return;
        }

        // B / Esc / Back also close here so gamepad does not depend only on the screen handler.
        if (commands.WasPressed(GameCommand.Cancel)
            || commands.WasPressed(GameCommand.Back)
            || commands.WasPressed(GameCommand.Info)
            || commands.WasPressed(GameCommand.Pause))
        {
            CloseDetail();
            return;
        }

        MaintainDetailFocus();
    }

    private void ApplyDetailFocusIndex()
    {
        if (_detailFocusable.Count == 0)
            return;

        _detailFocusIndex = Math.Clamp(_detailFocusIndex, 0, _detailFocusable.Count - 1);
        foreach (var (button, _) in _detailFocusable)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }

        _detailFocusable[_detailFocusIndex].Button.IsFocused = true;
    }

    private void MaintainDetailFocus()
    {
        for (var index = 0; index < _detailFocusable.Count; index++)
        {
            if (!_detailFocusable[index].Button.IsFocused)
                continue;
            _detailFocusIndex = index;
            return;
        }

        ApplyDetailFocusIndex();
    }

    private void AddBottomActions(Panel parent, ContentLibraryViewModel viewModel, Action onBack)
    {
        _actionBarHost = new Panel();
        GumUiLayout.FillParentWidth(_actionBarHost);
        _actionBarHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.AddChild(_actionBarHost);

        _actionFocusStartIndex = _focusableEntries.Count;

        // Install is also a top tab; this button is a shortcut to the same place.
        AddActionButton("Install", isEnabled: true, () => _onSelectTab?.Invoke(ContentLibraryTab.Install));
        AddActionButton("Download", viewModel.CanDownload, () => { });
        AddActionButton("Update", viewModel.CanUpdate, () => { });
        AddActionButton("Back", isEnabled: true, onBack);

        _actionFocusCount = _focusableEntries.Count - _actionFocusStartIndex;
    }

    private void AddActionButton(string text, bool isEnabled, Action onActivate)
    {
        var button = new Button { Text = text, IsEnabled = isEnabled };
        if (isEnabled)
        {
            button.Click += (_, _) => onActivate();
            _focusableEntries.Add((button, onActivate));
        }
        else
        {
            // Keep Gum gamepad focus out of disabled placeholders (Download / Update).
            button.Visual.HasEvents = false;
        }

        _actionButtons.Add(button);
    }

    private void AddListRow(string text, Action onActivate, bool isEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(_listPanel);

        var button = new Button { Text = text, IsEnabled = isEnabled };
        GumUiLayout.FillParentWidth(button);
        if (isEnabled)
        {
            button.Click += (_, _) => onActivate();
            _focusableEntries.Add((button, onActivate));
        }
        else
        {
            button.Visual.HasEvents = false;
        }

        _listPanel.AddChild(button);
    }

    private void AddTwoLineListRow(string titleLine, string detailLine, Action onActivate)
    {
        ArgumentNullException.ThrowIfNull(_listPanel);

        // Same pattern as shop offer rows: Dock.Fill button grows with the content shell,
        // so focus highlight covers both title and detail lines.
        const float padding = 6f;

        var row = new Panel();
        row.Visual.HasEvents = false;
        row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        GumUiLayout.FillParentWidth(row);
        _listPanel.AddChild(row);

        var button = new Button { Text = string.Empty, IsEnabled = true };
        button.Dock(Dock.Fill);
        button.Click += (_, _) => onActivate();
        row.AddChild(button);
        _focusableEntries.Add((button, onActivate));

        var shell = new Panel();
        shell.Visual.HasEvents = false;
        shell.Visual.X = padding;
        shell.Visual.Y = 0;
        shell.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        shell.Visual.Width = -(padding * 2);
        shell.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        shell.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        shell.Visual.StackSpacing = 2f;
        row.AddChild(shell);

        GumUiLayout.AddVerticalSpacer(shell, padding);

        var title = new Label { Text = titleLine };
        title.Visual.HasEvents = false;
        title.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        title.Visual.Width = 0;
        title.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        shell.AddChild(title);

        var detail = new Label { Text = detailLine };
        detail.Visual.HasEvents = false;
        detail.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        detail.Visual.Width = 0;
        detail.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        shell.AddChild(detail);

        GumUiLayout.AddVerticalSpacer(shell, padding);
    }

    private void AddEmptyHint(string text)
    {
        ArgumentNullException.ThrowIfNull(_listPanel);
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        _listPanel.AddChild(label);
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
        SyncFocusIndexFromUi();
        if (!IsOurFocusIntact())
            ApplyFocusIndex();
        else
        {
            _ownedFocusIndex = _focusIndex;
            EnsureFocusedRowVisible();
        }
    }

    private bool IsOurFocusIntact()
    {
        if (_focusIndex < 0 || _focusIndex >= _focusableEntries.Count)
            return false;
        return _focusableEntries[_focusIndex].Button.IsFocused;
    }

    private void SyncFocusIndexFromUi()
    {
        for (var index = 0; index < _focusableEntries.Count; index++)
        {
            if (!_focusableEntries[index].Button.IsFocused)
                continue;
            _focusIndex = index;
            return;
        }
    }

    private void ApplyFocusIndex()
    {
        if (_focusableEntries.Count == 0)
            return;

        StealFocusFromScrollChrome();
        _focusIndex = Math.Clamp(_focusIndex, 0, _focusableEntries.Count - 1);
        _ownedFocusIndex = _focusIndex;
        foreach (var (button, _) in _focusableEntries)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }

        _focusableEntries[_focusIndex].Button.IsFocused = true;
        EnsureFocusedRowVisible();
    }

    private void EnsureFocusedRowVisible()
    {
        if (_listScroll is null
            || _listFocusStartIndex < 0
            || _focusIndex < _listFocusStartIndex
            || _focusIndex >= _listFocusStartIndex + _listFocusCount)
        {
            return;
        }

        var rowIndex = _focusIndex - _listFocusStartIndex;
        var rowPitch = _activeTab == ContentLibraryTab.Bundles ? 72f : RowPitchEstimate;
        var target = rowIndex * rowPitch;
        var current = _listScroll.VerticalScrollBarValue;
        var viewHeight = _listScroll.Visual.Height > 1f ? _listScroll.Visual.Height : ListMaxHeight;
        if (target < current)
            _listScroll.VerticalScrollBarValue = target;
        else if (target + rowPitch > current + viewHeight)
            _listScroll.VerticalScrollBarValue = Math.Max(0, target + rowPitch - viewHeight);
    }

    private void StealFocusFromScrollChrome() => ClearScrollBarFocus(_listScroll);

    private static void ApplyListWellBackground(ScrollViewer scrollViewer)
    {
        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return;

        visual.BackgroundColor = ContentUiColors.ListWell;
    }

    private static void DisableScrollChromeFocus(ScrollViewer scrollViewer)
    {
        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return;

        if (visual.FocusedIndicator is not null)
            visual.FocusedIndicator.Visible = false;

        ClearScrollBarFocus(scrollViewer);
    }

    private static void ClearScrollBarFocus(ScrollViewer? scrollViewer)
    {
        if (scrollViewer?.Visual is not ScrollViewerVisual visual)
            return;

        if (scrollViewer.IsFocused)
            scrollViewer.IsFocused = false;

        ClearFormsFocus(visual.VerticalScrollBarInstance);
        ClearFormsFocus(visual.HorizontalScrollBarInstance);
    }

    private static void ClearFormsFocus(GraphicalUiElement? element)
    {
        if (element is ScrollBarVisual { FormsControl.IsFocused: true } scrollBarVisual)
            scrollBarVisual.FormsControl.IsFocused = false;
    }
}
