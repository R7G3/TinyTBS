using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>
/// New Game shell: Mode → Scenario → Level → Composition → Lobby tabs,
/// one tall scroll list, Start / Back.
/// Tab list bodies live in <c>NewGame*TabBody</c>;
/// scroll measure in <see cref="GumScrollListLayout"/>; focus chrome in <see cref="GumScrollViewerChrome"/>.
/// </summary>
public sealed class NewGameView
{
    private const float PanelMaxWidth = 760f;
    private const float ListMinHeight = 120f;
    private const float ListStackSpacing = 6f;
    private const float ListBottomPadding = GumScrollListLayout.DefaultListBottomPadding;
    private const float ChromeEstimateComfortable = 280f;
    private const float ChromeEstimateCompact = 240f;

    private static readonly NewGameTab[] TabOrder =
    [
        NewGameTab.Mode,
        NewGameTab.Scenario,
        NewGameTab.Level,
        NewGameTab.Composition,
        NewGameTab.Lobby,
    ];

    private Panel? _rootPanel;
    private Panel? _shell;
    private Label? _statusLabel;
    private Label? _hintsLabel;
    private ScrollViewer? _listScroll;
    private Panel? _listPanel;
    private Panel? _tabBarHost;
    private Panel? _actionBarHost;
    private NewGameTab _activeTab;
    private Action<NewGameTab>? _onSelectTab;
    private readonly List<Button> _tabButtons = [];
    private readonly List<Button> _actionButtons = [];
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly NewGameLobbyStepperInput _lobbySteppers = new();
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;
    private int _ownedFocusIndex;
    private int _listFocusStartIndex = -1;
    private int _listFocusCount;
    private int _actionFocusStartIndex = -1;
    private int _actionFocusCount;
    private int _addPlayerFocusIndex = -1;
    private int _addPlayerTypeLocalFocusIndex = -1;
    private int _cancelAddPlayerTypeFocusIndex = -1;
    private int _playerSlotFocusIndex = -1;
    private int _removePlayerFocusIndex = -1;
    private int _goldDecreaseFocusIndex = -1;
    private int _goldIncreaseFocusIndex = -1;
    private int _unitCapDecreaseFocusIndex = -1;
    private int _unitCapIncreaseFocusIndex = -1;
    private int _startFocusIndex = -1;
    private float _lastButtonBarWidth = -1f;

    public void Build(
        NewGameViewModel viewModel,
        Action<NewGameTab> onSelectTab,
        Action<NewGamePlayMode> onSelectMode,
        Action<string> onSelectScenario,
        Action<string> onSelectLevel,
        Action<string> onSelectComposition,
        Action onOpenAddPlayerChooser,
        Action onAddLocalPlayer,
        Action<TinyTBS.Game.Ai.BotDifficulty> onAddBotPlayer,
        Action onCancelAddPlayerChooser,
        Action<int> onRemovePlayerAt,
        Action<int> onActivatePlayerSlot,
        Action onDecreaseGold,
        Action onIncreaseGold,
        Action onDecreaseUnitCap,
        Action onIncreaseUnitCap,
        Action onStart,
        Action onBack,
        NewGameFocusAnchor focusAnchor = NewGameFocusAnchor.Auto)
    {
        Clear();

        _activeTab = viewModel.ActiveTab;
        _onSelectTab = onSelectTab;

        var canvasHeight = GumService.Default.CanvasHeight;
        var compact = canvasHeight > 0f && canvasHeight < 720f;
        var spacer = compact ? 6f : 12f;
        var stackSpacing = compact ? 5f : 8f;

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var bodyPanel = new Panel();
        bodyPanel.Dock(Dock.Fill);
        _rootPanel.AddChild(bodyPanel);

        _shell = new Panel();
        GumUiLayout.CenterInParent(_shell, xPercent: 50f, yPercent: 50f);
        GumUiLayout.SetBoundedWidth(_shell, PanelMaxWidth, parentPercent: 94f);
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
        GumScrollViewerChrome.DisableScrollChromeFocus(_listScroll);
        stack.AddChild(_listScroll);

        _listPanel = new Panel();
        _listPanel.Visual.HasEvents = false;
        _listPanel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _listPanel.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _listPanel.Visual.StackSpacing = ListStackSpacing;
        GumUiLayout.FillParentWidth(_listPanel);
        _listScroll.AddChild(_listPanel);

        _listFocusStartIndex = _focusableEntries.Count;
        PopulateActiveTab(
            viewModel,
            onSelectMode,
            onSelectScenario,
            onSelectLevel,
            onSelectComposition,
            onOpenAddPlayerChooser,
            onAddLocalPlayer,
            onAddBotPlayer,
            onCancelAddPlayerChooser,
            onRemovePlayerAt,
            onActivatePlayerSlot,
            onDecreaseGold,
            onIncreaseGold,
            onDecreaseUnitCap,
            onIncreaseUnitCap);
        GumUiLayout.AddVerticalSpacer(_listPanel, ListBottomPadding);
        _listFocusCount = _focusableEntries.Count - _listFocusStartIndex;

        AddBottomActions(stack, viewModel.CanStart, onStart, onBack);

        _hintsLabel = new Label { Text = BuildHintsText(compact) };
        GumUiLayout.FillParentWidth(_hintsLabel);
        stack.AddChild(_hintsLabel);

        GumUiLayout.AddVerticalSpacer(stack, spacer);

        ApplyResponsiveLayout();
        FocusInitial(viewModel, focusAnchor);
    }

    public void ApplyResponsiveLayout()
    {
        if (_listScroll is null || _shell is null)
            return;

        var canvasWidth = GumService.Default.CanvasWidth;
        var canvasHeight = GumService.Default.CanvasHeight;
        if (canvasWidth <= 0f || canvasHeight <= 0f)
            return;

        GumUiLayout.SetBoundedWidth(_shell, PanelMaxWidth, parentPercent: 94f);
        _shell.Visual.MaxHeight = canvasHeight * 0.96f;

        var panelInnerWidth = Math.Min(PanelMaxWidth, canvasWidth * 0.94f) * 0.94f;
        var chrome = canvasHeight < 720f || canvasWidth < 480f
            ? ChromeEstimateCompact
            : ChromeEstimateComfortable;
        var listHeight = Math.Max(ListMinHeight, canvasHeight * 0.94f - chrome);
        GumUiLayout.SetAbsoluteHeight(_listScroll, listHeight);

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
                    minButtonWidth: 72f,
                    preferredButtonWidth: 110f);
            }

            if (_actionBarHost is not null && _actionButtons.Count > 0)
            {
                GumUiLayout.LayoutAdaptiveButtonRows(
                    _actionBarHost,
                    _actionButtons,
                    panelInnerWidth,
                    spacing: 8f,
                    minButtonWidth: 88f,
                    preferredButtonWidth: 140f);
            }
        }

        if (_hintsLabel is not null)
            _hintsLabel.Text = BuildHintsText(canvasHeight < 720f || canvasWidth < 520f);

        GumScrollViewerChrome.DisableScrollChromeFocus(_listScroll);
    }

    public void SetStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text;
    }

    /// <summary>Updates gold / unit-cap captions without rebuilding (keeps focus and scroll).</summary>
    public void SyncLobbyValues(int startingGold, int unitCap) =>
        _lobbySteppers.SyncValues(startingGold, unitCap);

    public void HandleInput(
        IGameCommandSource commands,
        float elapsedSeconds,
        IPointerSource pointer)
    {
        if (_focusableEntries.Count == 0)
            return;

        _lobbySteppers.TickPointerHold(
            pointer,
            elapsedSeconds,
            ref _focusIndex,
            ApplyFocusIndex,
            _focusableEntries);
        _lobbySteppers.TryBeginPointerHold(
            pointer,
            ref _focusIndex,
            ApplyFocusIndex,
            _focusableEntries);

        if (commands.WasPressed(GameCommand.NavigateLeft))
        {
            _lobbySteppers.ClearPointerHold();
            _navigateRepeat.Reset();
            HandleHorizontalNavigate(-1);
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateRight))
        {
            _lobbySteppers.ClearPointerHold();
            _navigateRepeat.Reset();
            HandleHorizontalNavigate(+1);
            return;
        }

        var verticalDelta = _navigateRepeat.TryGetDelta(commands, elapsedSeconds);
        if (verticalDelta != 0)
        {
            _lobbySteppers.ClearPointerHold();
            RememberOwnedFocusFromUi();
            _focusIndex = Math.Clamp(_focusIndex + verticalDelta, 0, _focusableEntries.Count - 1);
            ApplyFocusIndex();
            return;
        }

        if (_lobbySteppers.TryHandleConfirmRepeat(commands, elapsedSeconds, _focusIndex, _focusableEntries))
            return;

        if (commands.WasPressed(GameCommand.Confirm))
        {
            _lobbySteppers.ClearPointerHold();
            RememberOwnedFocusFromUi();
            _focusableEntries[_focusIndex].Activate();
            return;
        }

        MaintainFocus();
    }

    public void HandlePointerScroll(int scrollWheelDelta)
    {
        if (_listScroll is null || scrollWheelDelta == 0)
            return;

        // MonoGame/Windows: one wheel notch ≈ 120 (WHEEL_DELTA). Treat that as one step.
        const int wheelDeltaPerNotch = 120;
        const float pixelsPerNotch = 48f;
        var notches = scrollWheelDelta / (float)wheelDeltaPerNotch;
        var next = _listScroll.VerticalScrollBarValue - notches * pixelsPerNotch;
        _listScroll.VerticalScrollBarValue = Math.Max(0f, next);
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
        _lobbySteppers.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;
        _ownedFocusIndex = 0;
        _listFocusStartIndex = -1;
        _listFocusCount = 0;
        _actionFocusStartIndex = -1;
        _actionFocusCount = 0;
        _addPlayerFocusIndex = -1;
        _addPlayerTypeLocalFocusIndex = -1;
        _cancelAddPlayerTypeFocusIndex = -1;
        _playerSlotFocusIndex = -1;
        _removePlayerFocusIndex = -1;
        _goldDecreaseFocusIndex = -1;
        _goldIncreaseFocusIndex = -1;
        _unitCapDecreaseFocusIndex = -1;
        _unitCapIncreaseFocusIndex = -1;
        _startFocusIndex = -1;
        _lastButtonBarWidth = -1f;
    }

    private void PopulateActiveTab(
        NewGameViewModel viewModel,
        Action<NewGamePlayMode> onSelectMode,
        Action<string> onSelectScenario,
        Action<string> onSelectLevel,
        Action<string> onSelectComposition,
        Action onOpenAddPlayerChooser,
        Action onAddLocalPlayer,
        Action<TinyTBS.Game.Ai.BotDifficulty> onAddBotPlayer,
        Action onCancelAddPlayerChooser,
        Action<int> onRemovePlayerAt,
        Action<int> onActivatePlayerSlot,
        Action onDecreaseGold,
        Action onIncreaseGold,
        Action onDecreaseUnitCap,
        Action onIncreaseUnitCap)
    {
        ArgumentNullException.ThrowIfNull(_listPanel);

        var list = new NewGameListBuilder(_listPanel, _focusableEntries);

        switch (viewModel.ActiveTab)
        {
            case NewGameTab.Mode:
                NewGameModeTabBody.Populate(list, viewModel, onSelectMode);
                break;
            case NewGameTab.Scenario:
                NewGameScenarioTabBody.Populate(list, viewModel, onSelectScenario);
                break;
            case NewGameTab.Level:
                NewGameLevelTabBody.Populate(list, viewModel, onSelectLevel);
                break;
            case NewGameTab.Composition:
                NewGameCompositionTabBody.Populate(list, viewModel, onSelectComposition);
                break;
            case NewGameTab.Lobby:
                ApplyLobbyResult(
                    NewGameLobbyTabBody.Populate(
                        list,
                        viewModel,
                        onOpenAddPlayerChooser,
                        onAddLocalPlayer,
                        onAddBotPlayer,
                        onCancelAddPlayerChooser,
                        onRemovePlayerAt,
                        onActivatePlayerSlot,
                        onDecreaseGold,
                        onIncreaseGold,
                        onDecreaseUnitCap,
                        onIncreaseUnitCap));
                break;
        }
    }

    private void ApplyLobbyResult(NewGameLobbyTabBodyResult result)
    {
        _addPlayerFocusIndex = result.AddPlayerFocusIndex;
        _addPlayerTypeLocalFocusIndex = result.AddPlayerTypeLocalFocusIndex;
        _cancelAddPlayerTypeFocusIndex = result.CancelAddPlayerTypeFocusIndex;
        _playerSlotFocusIndex = result.FirstPlayerSlotFocusIndex;
        _removePlayerFocusIndex = result.FirstRemovePlayerFocusIndex;
        _goldDecreaseFocusIndex = result.GoldStepper?.DecreaseFocusIndex ?? -1;
        _goldIncreaseFocusIndex = result.GoldStepper?.IncreaseFocusIndex ?? -1;
        _unitCapDecreaseFocusIndex = result.UnitCapStepper?.DecreaseFocusIndex ?? -1;
        _unitCapIncreaseFocusIndex = result.UnitCapStepper?.IncreaseFocusIndex ?? -1;
        _lobbySteppers.Bind(result);
    }

    private void AddTabBar(Panel parent, NewGameTab activeTab)
    {
        _tabBarHost = GumMenuChrome.CreateButtonBarHost(parent);

        GumMenuChrome.AddTabButton(_tabButtons, _focusableEntries, "Mode", activeTab == NewGameTab.Mode, () => _onSelectTab?.Invoke(NewGameTab.Mode));
        GumMenuChrome.AddTabButton(_tabButtons, _focusableEntries, "Scenario", activeTab == NewGameTab.Scenario, () => _onSelectTab?.Invoke(NewGameTab.Scenario));
        GumMenuChrome.AddTabButton(_tabButtons, _focusableEntries, "Level", activeTab == NewGameTab.Level, () => _onSelectTab?.Invoke(NewGameTab.Level));
        GumMenuChrome.AddTabButton(_tabButtons, _focusableEntries, "Composition", activeTab == NewGameTab.Composition, () => _onSelectTab?.Invoke(NewGameTab.Composition));
        GumMenuChrome.AddTabButton(_tabButtons, _focusableEntries, "Lobby", activeTab == NewGameTab.Lobby, () => _onSelectTab?.Invoke(NewGameTab.Lobby));
    }

    private void AddBottomActions(Panel parent, bool canStart, Action onStart, Action onBack)
    {
        _actionBarHost = GumMenuChrome.CreateButtonBarHost(parent);

        _actionFocusStartIndex = _focusableEntries.Count;
        GumMenuChrome.AddActionButton(_actionButtons, _focusableEntries, "Start", canStart, onStart, index => _startFocusIndex = index);
        GumMenuChrome.AddActionButton(_actionButtons, _focusableEntries, "Back", true, onBack);
        _actionFocusCount = _focusableEntries.Count - _actionFocusStartIndex;
    }

    private static string BuildHintsText(bool compact) =>
        compact
            ? "Up/Down  ·  Confirm  ·  L/R tabs or actions  ·  Esc/B"
            : "Up/Down list  ·  Confirm  ·  Left/Right: tabs (or bottom buttons)  ·  Esc / B back";

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

        var currentIndex = Array.IndexOf(TabOrder, _activeTab);
        if (currentIndex < 0)
            return;

        var nextIndex = Math.Clamp(currentIndex + delta, 0, TabOrder.Length - 1);
        var next = TabOrder[nextIndex];
        if (next != _activeTab)
            _onSelectTab(next);
    }

    private void FocusInitial(NewGameViewModel viewModel, NewGameFocusAnchor focusAnchor)
    {
        if (_focusableEntries.Count == 0)
            return;

        if (TryFocusIndex(ResolveFocusAnchorIndex(viewModel, focusAnchor)))
            return;

        if (_listFocusCount > 0 && _listFocusStartIndex >= 0)
            _focusIndex = _listFocusStartIndex;
        else
            _focusIndex = 0;

        ApplyFocusIndex();
    }

    private int ResolveFocusAnchorIndex(NewGameViewModel viewModel, NewGameFocusAnchor focusAnchor)
    {
        switch (focusAnchor)
        {
            case NewGameFocusAnchor.SelectedMode:
                return FindSelectedModeFocusIndex(viewModel);
            case NewGameFocusAnchor.SelectedScenario:
                return FindSelectedScenarioFocusIndex(viewModel);
            case NewGameFocusAnchor.SelectedLevel:
                return FindSelectedLevelFocusIndex(viewModel);
            case NewGameFocusAnchor.SelectedComposition:
                return FindSelectedCompositionFocusIndex(viewModel);
            case NewGameFocusAnchor.AddPlayer:
                return _addPlayerFocusIndex;
            case NewGameFocusAnchor.AddPlayerTypeLocal:
                return _addPlayerTypeLocalFocusIndex;
            case NewGameFocusAnchor.CancelAddPlayerType:
                return _cancelAddPlayerTypeFocusIndex;
            case NewGameFocusAnchor.RemovePlayer:
                return _playerSlotFocusIndex >= 0 ? _playerSlotFocusIndex : _removePlayerFocusIndex;
            case NewGameFocusAnchor.GoldDecrease:
                return _goldDecreaseFocusIndex;
            case NewGameFocusAnchor.GoldIncrease:
                return _goldIncreaseFocusIndex;
            case NewGameFocusAnchor.UnitCapDecrease:
                return _unitCapDecreaseFocusIndex;
            case NewGameFocusAnchor.UnitCapIncrease:
                return _unitCapIncreaseFocusIndex;
            case NewGameFocusAnchor.Start:
                return _startFocusIndex;
            default:
                return viewModel.ActiveTab switch
                {
                    NewGameTab.Mode => FindSelectedModeFocusIndex(viewModel),
                    NewGameTab.Scenario => FindSelectedScenarioFocusIndex(viewModel),
                    NewGameTab.Level => FindSelectedLevelFocusIndex(viewModel),
                    NewGameTab.Composition => FindSelectedCompositionFocusIndex(viewModel),
                    NewGameTab.Lobby => PreferLobbyFocusIndex(),
                    _ => -1,
                };
        }
    }

    private int PreferLobbyFocusIndex()
    {
        if (_addPlayerTypeLocalFocusIndex >= 0)
            return _addPlayerTypeLocalFocusIndex;
        // Prefer first slot caption so Players header stays in view (cycle bot / X nearby).
        if (_playerSlotFocusIndex >= 0)
            return _playerSlotFocusIndex;
        if (_removePlayerFocusIndex >= 0)
            return _removePlayerFocusIndex;
        if (_addPlayerFocusIndex >= 0)
            return _addPlayerFocusIndex;
        if (_goldDecreaseFocusIndex >= 0)
            return _goldDecreaseFocusIndex;
        return -1;
    }

    private int FindSelectedModeFocusIndex(NewGameViewModel viewModel)
    {
        for (var index = 0; index < viewModel.Modes.Count; index++)
        {
            if (!viewModel.Modes[index].IsSelected)
                continue;
            return FindNthListFocusable(index);
        }

        return -1;
    }

    private int FindNthListFocusable(int listRowIndex)
    {
        if (_listFocusStartIndex < 0 || listRowIndex < 0 || listRowIndex >= _listFocusCount)
            return -1;
        return _listFocusStartIndex + listRowIndex;
    }

    private int FindSelectedScenarioFocusIndex(NewGameViewModel viewModel)
    {
        for (var index = 0; index < viewModel.Scenarios.Count; index++)
        {
            if (!viewModel.Scenarios[index].IsSelected)
                continue;
            return FindNthListFocusable(index);
        }

        return -1;
    }

    private int FindSelectedLevelFocusIndex(NewGameViewModel viewModel)
    {
        for (var index = 0; index < viewModel.Levels.Count; index++)
        {
            if (!viewModel.Levels[index].IsSelected)
                continue;
            return FindNthListFocusable(index);
        }

        return -1;
    }

    private int FindSelectedCompositionFocusIndex(NewGameViewModel viewModel)
    {
        for (var index = 0; index < viewModel.CompositionOptions.Count; index++)
        {
            if (!viewModel.CompositionOptions[index].IsSelected)
                continue;
            return FindNthListFocusable(index);
        }

        return -1;
    }

    private bool TryFocusIndex(int focusIndex)
    {
        if (focusIndex < 0 || focusIndex >= _focusableEntries.Count)
            return false;

        _focusIndex = focusIndex;
        ApplyFocusIndex();
        return true;
    }

    private void MaintainFocus()
    {
        GumScrollViewerChrome.StealFocusFromScrollChrome(_listScroll);
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

        GumScrollViewerChrome.StealFocusFromScrollChrome(_listScroll);
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
        _ownedFocusIndex = _focusIndex;

        if (_listScroll is not null && _listPanel is not null)
        {
            GumScrollListLayout.EnsureFocusedRowVisible(
                _listScroll,
                _listPanel,
                _focusableEntries[_focusIndex].Button.Visual,
                _listFocusStartIndex,
                _listFocusCount,
                _focusIndex,
                ListStackSpacing,
                ListMinHeight,
                listBottomPadding: ListBottomPadding);
        }
    }
}
