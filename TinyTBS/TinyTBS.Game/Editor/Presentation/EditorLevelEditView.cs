using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Editor.Levels;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>
/// Edit level.json: settings list (left) + action menu (right), campaign-style columns.
/// </summary>
public sealed class EditorLevelEditView
{
    private enum FocusZone
    {
        Settings = 0,
        Menu = 1,
    }

    private const float TextFieldHeight = 36f;
    private const float StackSpacing = 6f;
    private const float MinScrollViewport = 120f;
    private const float SettingsListMinHeight = 260f;

    private Panel? _rootPanel;
    private ScrollViewer? _settingsScroll;
    private Panel? _settingsHost;
    private Label? _statusLabel;
    private Label? _mapLabel;
    private Label? _playersLabel;
    private Label? _goldLabel;
    private Label? _capLabel;
    private Label? _modesLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private readonly List<(Button Button, Action Activate)> _settingsEntries = [];
    private readonly List<(Button Button, Action Activate)> _menuEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private FocusZone _focusZone = FocusZone.Settings;
    private int _settingsFocusIndex;
    private int _menuFocusIndex;
    private EditableLevelDocument _document = EditableLevelDocument.CreateDefault("level", "level", "map");
    private IReadOnlyList<string> _mapIds = [];
    private int _mapIndex;

    private float _confirmRepeatTimer;
    private float _pointerRepeatTimer;
    private int _pointerHoldFocusIndex = -1;
    private Button? _minDecrease;
    private Button? _minIncrease;
    private Button? _maxDecrease;
    private Button? _maxIncrease;
    private Button? _slotsDecrease;
    private Button? _slotsIncrease;
    private Button? _goldDecrease;
    private Button? _goldIncrease;
    private Button? _capDecrease;
    private Button? _capIncrease;
    private (Button Decrease, Button Increase)[] _stepperRows = [];

    public bool IsTextEntryActive =>
        _idBox is { IsFocused: true } || _titleBox is { IsFocused: true };

    public EditableLevelDocument Document => _document;

    public void Build(
        EditableLevelDocument document,
        IReadOnlyList<string> mapIds,
        Action onSave,
        Action onBack)
    {
        Clear();
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _mapIds = mapIds ?? [];
        _mapIndex = IndexOfMap(document.MapIdFromRef());

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var body = new Panel();
        body.Dock(Dock.Fill);
        _rootPanel.AddChild(body);

        var shell = new Panel();
        GumUiLayout.CenterHorizontallyInParent(shell);
        shell.Visual.Y = 12f;
        GumUiLayout.SetBoundedWidth(shell, maxPixels: 760f, parentPercent: 96f);
        var canvasHeight = Math.Max(320f, GumService.Default.CanvasHeight);
        // Absolute shell height so the settings ScrollViewer cannot paint past the panel edge.
        var shellHeight = Math.Max(320f, canvasHeight - 24f);
        const float topChrome = 10f + 26f + 26f + StackSpacing * 3f;
        const float bottomChrome = 10f;
        const float columnHeader = 26f + 4f;
        var listHeight = Math.Max(SettingsListMinHeight, shellHeight - topChrome - bottomChrome - columnHeader);
        GumUiLayout.SetAbsoluteHeight(shell, topChrome + bottomChrome + columnHeader + listHeight);
        body.AddChild(shell);
        GumUiLayout.AddSolidBackground(shell, EditorUiColors.Panel);

        var rootStack = GumUiLayout.CreateVerticalStackPanel(spacing: StackSpacing, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(rootStack);
        shell.AddChild(rootStack);
        GumUiLayout.AddVerticalSpacer(rootStack, 10f);

        var heading = new Label { Text = "Edit Level" };
        GumUiLayout.FillParentWidth(heading);
        rootStack.AddChild(heading);

        var hint = new Label
        {
            Text = "LB/RB: columns. Left/Right: −/+. Up/Down: next stepper row.",
        };
        GumUiLayout.FillParentWidth(hint);
        rootStack.AddChild(hint);

        var columns = new Panel();
        GumUiLayout.FillParentWidth(columns);
        columns.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        columns.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        columns.Visual.StackSpacing = 10f;
        rootStack.AddChild(columns);

        var leftColumn = CreateColumn(columns, "Settings", listHeight, out _settingsHost, out _settingsScroll);
        var rightColumn = CreateMenuColumn(columns, listHeight, onSave, onBack);
        GumUiLayout.SetWidthPercent(leftColumn, 62f);
        GumUiLayout.SetWidthPercent(rightColumn, 36f);

        BuildSettingsColumn(document);
        GumUiLayout.AddVerticalSpacer(rootStack, 10f);
        SetFocusZone(FocusZone.Settings, resetIndex: true);
    }

    public void SyncStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text ?? string.Empty;
    }

    public void ApplyTextFields()
    {
        if (_idBox is not null && !string.IsNullOrWhiteSpace(_idBox.Text))
            _document.Id = _idBox.Text.Trim();
        if (_titleBox is not null)
            _document.Title = string.IsNullOrWhiteSpace(_titleBox.Text) ? _document.Id : _titleBox.Text.Trim();
        _document.IsDirty = true;
    }

    public void HandleInput(IGameCommandSource commands, IPointerSource pointer, float elapsedSeconds)
    {
        if (IsTextEntryActive)
            return;

        TickPointerHold(pointer, elapsedSeconds);
        TryBeginPointerHold(pointer);

        if (TrySwitchColumn(commands))
            return;

        if (_focusZone == FocusZone.Menu)
        {
            HandleMenuInput(commands, elapsedSeconds);
            return;
        }

        HandleSettingsInput(commands, elapsedSeconds);
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _settingsScroll = null;
        _settingsHost = null;
        _statusLabel = null;
        _mapLabel = null;
        _playersLabel = null;
        _goldLabel = null;
        _capLabel = null;
        _modesLabel = null;
        _idBox = null;
        _titleBox = null;
        _settingsEntries.Clear();
        _menuEntries.Clear();
        _navigateRepeat.Reset();
        _focusZone = FocusZone.Settings;
        _settingsFocusIndex = 0;
        _menuFocusIndex = 0;
        _mapIds = [];
        _mapIndex = 0;
        _confirmRepeatTimer = 0f;
        ClearPointerHold();
        _minDecrease = null;
        _minIncrease = null;
        _maxDecrease = null;
        _maxIncrease = null;
        _slotsDecrease = null;
        _slotsIncrease = null;
        _goldDecrease = null;
        _goldIncrease = null;
        _capDecrease = null;
        _capIncrease = null;
        _stepperRows = [];
    }

    private void BuildSettingsColumn(EditableLevelDocument document)
    {
        if (_settingsHost is null)
            return;

        AddTextField(_settingsHost, "Level Id (Levels/ folder name)", document.Id, out _idBox);
        AddTextField(_settingsHost, "Title (display name)", document.Title, out _titleBox);

        _mapLabel = new Label { Text = MapCaption() };
        GumUiLayout.FillParentWidth(_mapLabel);
        _settingsHost.AddChild(_mapLabel);
        AddSettingsButton("Prev Map", () => CycleMap(-1));
        AddSettingsButton("Next Map", () => CycleMap(+1));

        _playersLabel = new Label { Text = PlayersCaption() };
        GumUiLayout.FillParentWidth(_playersLabel);
        _settingsHost.AddChild(_playersLabel);
        AddStepperRow(_settingsHost, () => AdjustPlayersMin(-1), () => AdjustPlayersMin(+1), out _minDecrease, out _minIncrease, "Min players");
        AddStepperRow(_settingsHost, () => AdjustPlayersMax(-1), () => AdjustPlayersMax(+1), out _maxDecrease, out _maxIncrease, "Max players");
        AddStepperRow(_settingsHost, () => AdjustDefaultSlots(-1), () => AdjustDefaultSlots(+1), out _slotsDecrease, out _slotsIncrease, "Default slots");

        _goldLabel = new Label { Text = GoldCaption() };
        GumUiLayout.FillParentWidth(_goldLabel);
        _settingsHost.AddChild(_goldLabel);
        AddStepperRow(_settingsHost, () => AdjustGold(-50), () => AdjustGold(+50), out _goldDecrease, out _goldIncrease);

        _capLabel = new Label { Text = CapCaption() };
        GumUiLayout.FillParentWidth(_capLabel);
        _settingsHost.AddChild(_capLabel);
        AddStepperRow(_settingsHost, () => AdjustCap(-1), () => AdjustCap(+1), out _capDecrease, out _capIncrease);

        _stepperRows =
        [
            (_minDecrease!, _minIncrease!),
            (_maxDecrease!, _maxIncrease!),
            (_slotsDecrease!, _slotsIncrease!),
            (_goldDecrease!, _goldIncrease!),
            (_capDecrease!, _capIncrease!),
        ];
    }

    private Panel CreateMenuColumn(Panel columns, float listHeight, Action onSave, Action onBack)
    {
        var column = new Panel();
        column.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        column.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        column.Visual.StackSpacing = 4f;
        columns.AddChild(column);

        var header = new Label { Text = "Menu" };
        GumUiLayout.FillParentWidth(header);
        column.AddChild(header);

        var host = new Panel();
        GumUiLayout.FillParentWidth(host);
        host.Visual.Height = listHeight;
        host.Visual.HeightUnits = DimensionUnitType.Absolute;
        host.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        host.Visual.StackSpacing = 6f;
        column.AddChild(host);

        _modesLabel = new Label { Text = ModesCaption() };
        GumUiLayout.FillParentWidth(_modesLabel);
        host.AddChild(_modesLabel);
        AddMenuButton(host, "Toggle Skirmish", () => ToggleMode("skirmish"));
        AddMenuButton(host, "Toggle Campaign", () => ToggleMode("campaign"));

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        host.AddChild(_statusLabel);

        AddMenuButton(host, "Save", () =>
        {
            ApplyTextFields();
            onSave();
        });
        AddMenuButton(host, "Back", onBack);
        return column;
    }

    private static Panel CreateColumn(
        Panel columns,
        string headerText,
        float listHeight,
        out Panel host,
        out ScrollViewer scroll)
    {
        var column = new Panel();
        column.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        column.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        column.Visual.StackSpacing = 4f;
        columns.AddChild(column);

        var header = new Label { Text = headerText };
        GumUiLayout.FillParentWidth(header);
        column.AddChild(header);

        scroll = new ScrollViewer();
        GumUiLayout.FillParentWidth(scroll);
        scroll.Visual.Height = listHeight;
        scroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        scroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        scroll.InnerPanel.Width = 0;
        scroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        GumScrollViewerChrome.DisableScrollChromeFocus(scroll);
        column.AddChild(scroll);

        host = new Panel();
        host.Visual.HasEvents = false;
        host.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        host.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        host.Visual.StackSpacing = StackSpacing;
        GumUiLayout.FillParentWidth(host);
        scroll.AddChild(host);
        return column;
    }

    private void HandleSettingsInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_settingsEntries.Count == 0)
            return;

        if (IsStepperFocused()
            && HeldCommandRepeat.TryTick(commands, GameCommand.Confirm, elapsedSeconds, ref _confirmRepeatTimer))
        {
            _settingsEntries[_settingsFocusIndex].Activate();
            return;
        }

        if (!IsStepperFocused())
            _confirmRepeatTimer = 0f;

        if (IsStepperFocused())
        {
            var horizontal = _navigateRepeat.TryGetHorizontalDelta(commands, elapsedSeconds);
            if (horizontal != 0 && TryMoveWithinStepperRow(horizontal))
            {
                EnsureSettingsRowVisible();
                return;
            }

            var vertical = _navigateRepeat.TryGetDelta(commands, elapsedSeconds);
            if (vertical != 0)
            {
                TryMoveStepperRowVertically(vertical);
                EnsureSettingsRowVisible();
                return;
            }

            if (commands.WasPressed(GameCommand.Confirm))
            {
                _settingsEntries[_settingsFocusIndex].Activate();
                return;
            }

            GumFocusableButtonList.MaintainFocus(_settingsEntries, ref _settingsFocusIndex);
            return;
        }

        var result = GumFocusableButtonList.HandleVerticalInput(
            commands,
            _settingsEntries,
            ref _settingsFocusIndex,
            _navigateRepeat,
            elapsedSeconds);

        if (result == GumFocusListResult.Navigated)
            EnsureSettingsRowVisible();
    }

    private void HandleMenuInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_menuEntries.Count == 0)
            return;

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _menuEntries,
            ref _menuFocusIndex,
            _navigateRepeat,
            elapsedSeconds);
    }

    private bool TrySwitchColumn(IGameCommandSource commands)
    {
        // Shoulders only — D-pad Left/Right stays free for −/+ steppers.
        var toMenu = commands.WasPressed(GameCommand.FocusNextRegion)
            || commands.WasPressed(GameCommand.ZoomIn);
        var toSettings = commands.WasPressed(GameCommand.FocusPreviousRegion)
            || commands.WasPressed(GameCommand.ZoomOut);

        if (toMenu && _focusZone == FocusZone.Settings)
        {
            SetFocusZone(FocusZone.Menu, resetIndex: false);
            return true;
        }

        if (toSettings && _focusZone == FocusZone.Menu)
        {
            SetFocusZone(FocusZone.Settings, resetIndex: false);
            return true;
        }

        return (toMenu && _focusZone == FocusZone.Menu) || (toSettings && _focusZone == FocusZone.Settings);
    }

    private bool TryMoveWithinStepperRow(int horizontalDelta)
    {
        if (!TryGetStepperPlacement(out var rowIndex, out var onIncrease))
            return false;

        var targetIncrease = horizontalDelta > 0;
        if (targetIncrease == onIncrease)
            return true;

        return FocusStepper(rowIndex, targetIncrease);
    }

    /// <summary>+1 down / -1 up between −/+ rows, keeping − or + side.</summary>
    private void TryMoveStepperRowVertically(int verticalDelta)
    {
        if (!TryGetStepperPlacement(out var rowIndex, out var onIncrease))
            return;

        var nextRow = rowIndex + verticalDelta;
        if (nextRow >= 0 && nextRow < _stepperRows.Length)
        {
            FocusStepper(nextRow, onIncrease);
            return;
        }

        // Up from the first stepper row → last control above steppers (Next Map).
        if (verticalDelta < 0 && rowIndex == 0 && _stepperRows.Length > 0)
        {
            var firstStepperIndex = IndexOfSettingsButton(_stepperRows[0].Decrease);
            if (firstStepperIndex > 0)
            {
                _settingsFocusIndex = firstStepperIndex - 1;
                GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
            }
        }
    }

    private bool TryGetStepperPlacement(out int rowIndex, out bool onIncrease)
    {
        rowIndex = -1;
        onIncrease = false;
        if (_settingsFocusIndex < 0 || _settingsFocusIndex >= _settingsEntries.Count)
            return false;

        var button = _settingsEntries[_settingsFocusIndex].Button;
        for (var i = 0; i < _stepperRows.Length; i++)
        {
            if (ReferenceEquals(button, _stepperRows[i].Decrease))
            {
                rowIndex = i;
                onIncrease = false;
                return true;
            }

            if (ReferenceEquals(button, _stepperRows[i].Increase))
            {
                rowIndex = i;
                onIncrease = true;
                return true;
            }
        }

        return false;
    }

    private bool FocusStepper(int rowIndex, bool onIncrease)
    {
        if (rowIndex < 0 || rowIndex >= _stepperRows.Length)
            return false;

        var target = onIncrease ? _stepperRows[rowIndex].Increase : _stepperRows[rowIndex].Decrease;
        var index = IndexOfSettingsButton(target);
        if (index < 0)
            return false;

        _settingsFocusIndex = index;
        GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
        return true;
    }

    private int IndexOfSettingsButton(Button button)
    {
        for (var i = 0; i < _settingsEntries.Count; i++)
        {
            if (ReferenceEquals(_settingsEntries[i].Button, button))
                return i;
        }

        return -1;
    }

    private void SetFocusZone(FocusZone zone, bool resetIndex)
    {
        _focusZone = zone;
        if (zone == FocusZone.Settings)
        {
            if (resetIndex || _settingsEntries.Count == 0)
                _settingsFocusIndex = 0;
            else
                _settingsFocusIndex = Math.Clamp(_settingsFocusIndex, 0, _settingsEntries.Count - 1);

            GumFocusableButtonList.ClearFocus(_menuEntries);
            if (_settingsEntries.Count > 0)
            {
                GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
                EnsureSettingsRowVisible();
            }
        }
        else
        {
            if (resetIndex || _menuEntries.Count == 0)
                _menuFocusIndex = 0;
            else
                _menuFocusIndex = Math.Clamp(_menuFocusIndex, 0, _menuEntries.Count - 1);

            GumFocusableButtonList.ClearFocus(_settingsEntries);
            if (_menuEntries.Count > 0)
                GumFocusableButtonList.ApplyFocus(_menuEntries, ref _menuFocusIndex);
        }
    }

    private void EnsureSettingsRowVisible()
    {
        if (_settingsEntries.Count == 0
            || _settingsScroll is null
            || _settingsHost is null)
        {
            return;
        }

        GumScrollViewerChrome.StealFocusFromScrollChrome(_settingsScroll);
        GumScrollListLayout.EnsureFocusedRowVisible(
            _settingsScroll,
            _settingsHost,
            _settingsEntries[_settingsFocusIndex].Button.Visual,
            listFocusStartIndex: 0,
            listFocusCount: _settingsEntries.Count,
            _settingsFocusIndex,
            StackSpacing,
            MinScrollViewport,
            scrollIntoViewMargin: 10f,
            listBottomPadding: 28f);
    }

    private void CycleMap(int delta)
    {
        if (_mapIds.Count == 0)
            return;
        _mapIndex = (_mapIndex + delta + _mapIds.Count) % _mapIds.Count;
        _document.SetMapId(_mapIds[_mapIndex]);
        if (_mapLabel is not null)
            _mapLabel.Text = MapCaption();
    }

    private void AdjustPlayersMin(int delta)
    {
        var min = Math.Clamp(_document.PlayersMin + delta, 1, 8);
        _document.PlayersMin = min;
        if (_document.PlayersMax < min)
            _document.PlayersMax = min;
        if (_document.DefaultSlots < min)
            _document.DefaultSlots = min;
        if (_document.DefaultSlots > _document.PlayersMax)
            _document.DefaultSlots = _document.PlayersMax;
        _document.IsDirty = true;
        RefreshPlayerLabel();
    }

    private void AdjustPlayersMax(int delta)
    {
        var max = Math.Clamp(_document.PlayersMax + delta, 1, 8);
        _document.PlayersMax = max;
        if (_document.PlayersMin > max)
            _document.PlayersMin = max;
        if (_document.DefaultSlots > max)
            _document.DefaultSlots = max;
        if (_document.DefaultSlots < _document.PlayersMin)
            _document.DefaultSlots = _document.PlayersMin;
        _document.IsDirty = true;
        RefreshPlayerLabel();
    }

    private void AdjustDefaultSlots(int delta)
    {
        var slots = Math.Clamp(_document.DefaultSlots + delta, _document.PlayersMin, _document.PlayersMax);
        _document.DefaultSlots = slots;
        _document.IsDirty = true;
        RefreshPlayerLabel();
    }

    private void AdjustGold(int delta)
    {
        _document.DefaultStartingGold = Math.Max(0, _document.DefaultStartingGold + delta);
        _document.IsDirty = true;
        if (_goldLabel is not null)
            _goldLabel.Text = GoldCaption();
    }

    private void AdjustCap(int delta)
    {
        _document.DefaultUnitCap = Math.Max(1, _document.DefaultUnitCap + delta);
        _document.IsDirty = true;
        if (_capLabel is not null)
            _capLabel.Text = CapCaption();
    }

    private void RefreshPlayerLabel()
    {
        if (_playersLabel is not null)
            _playersLabel.Text = PlayersCaption();
    }

    private void ToggleMode(string mode)
    {
        _document.SetMode(mode, !_document.HasMode(mode));
        if (_modesLabel is not null)
            _modesLabel.Text = ModesCaption();
    }

    private int IndexOfMap(string mapId)
    {
        for (var i = 0; i < _mapIds.Count; i++)
        {
            if (string.Equals(_mapIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return 0;
    }

    private string MapCaption() =>
        _mapIds.Count == 0
            ? "Map: (no maps — create a map first)"
            : $"Map: {_document.MapRef} ({_mapIndex + 1}/{_mapIds.Count})";

    private string PlayersCaption() =>
        $"Players: min {_document.PlayersMin}, max {_document.PlayersMax}, default slots {_document.DefaultSlots}";

    private string GoldCaption() => $"Starting gold: {_document.DefaultStartingGold}";

    private string CapCaption() => $"Unit cap: {_document.DefaultUnitCap}";

    private string ModesCaption() => "Modes: " + string.Join(", ", _document.Modes);

    private static void AddTextField(Panel parent, string caption, string initialText, out TextBox textBox)
    {
        var label = new Label { Text = caption };
        GumUiLayout.FillParentWidth(label);
        parent.AddChild(label);
        textBox = new TextBox { Text = initialText };
        GumUiLayout.FillParentWidth(textBox);
        GumUiLayout.SetAbsoluteHeight(textBox, TextFieldHeight);
        EditorTextFieldStyle.Apply(textBox);
        parent.AddChild(textBox);
    }

    private void AddSettingsButton(string text, Action onClick)
    {
        if (_settingsHost is null)
            return;
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) =>
        {
            _settingsFocusIndex = _settingsEntries.Count;
            SetFocusZone(FocusZone.Settings, resetIndex: false);
            onClick();
        };
        _settingsHost.AddChild(button);
        _settingsEntries.Add((button, onClick));
    }

    private void AddMenuButton(Panel parent, string text, Action onClick)
    {
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) =>
        {
            for (var i = 0; i < _menuEntries.Count; i++)
            {
                if (!ReferenceEquals(_menuEntries[i].Button, button))
                    continue;
                _menuFocusIndex = i;
                break;
            }

            SetFocusZone(FocusZone.Menu, resetIndex: false);
            onClick();
        };
        parent.AddChild(button);
        _menuEntries.Add((button, onClick));
    }

    private void AddStepperRow(
        Panel parent,
        Action onDecrease,
        Action onIncrease,
        out Button decreaseButton,
        out Button increaseButton,
        string? rowCaption = null)
    {
        if (!string.IsNullOrWhiteSpace(rowCaption))
        {
            var caption = new Label { Text = rowCaption };
            GumUiLayout.FillParentWidth(caption);
            parent.AddChild(caption);
        }

        var row = new Panel();
        GumUiLayout.FillParentWidth(row);
        row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.Visual.StackSpacing = 8f;
        parent.AddChild(row);

        decreaseButton = CreateStepperButton(row, "-", onDecrease);
        increaseButton = CreateStepperButton(row, "+", onIncrease);
    }

    private Button CreateStepperButton(Panel row, string text, Action onActivate)
    {
        var button = new Button { Text = text };
        GumUiLayout.SetAbsoluteWidth(button, 72f);
        row.AddChild(button);
        _settingsEntries.Add((button, onActivate));
        return button;
    }

    private bool IsStepperFocused()
    {
        if (_focusZone != FocusZone.Settings
            || _settingsFocusIndex < 0
            || _settingsFocusIndex >= _settingsEntries.Count)
        {
            return false;
        }

        var button = _settingsEntries[_settingsFocusIndex].Button;
        return ReferenceEquals(button, _minDecrease)
            || ReferenceEquals(button, _minIncrease)
            || ReferenceEquals(button, _maxDecrease)
            || ReferenceEquals(button, _maxIncrease)
            || ReferenceEquals(button, _slotsDecrease)
            || ReferenceEquals(button, _slotsIncrease)
            || ReferenceEquals(button, _goldDecrease)
            || ReferenceEquals(button, _goldIncrease)
            || ReferenceEquals(button, _capDecrease)
            || ReferenceEquals(button, _capIncrease);
    }

    private void TryBeginPointerHold(IPointerSource pointer)
    {
        if (!pointer.WasPrimaryPressed)
            return;

        if (!TryFindStepperUnderPointer(out var focusIndex))
            return;

        _settingsFocusIndex = focusIndex;
        SetFocusZone(FocusZone.Settings, resetIndex: false);
        _settingsEntries[focusIndex].Activate();
        _pointerHoldFocusIndex = focusIndex;
        _pointerRepeatTimer = HeldCommandRepeat.DefaultInitialDelaySeconds;
    }

    private void TickPointerHold(IPointerSource pointer, float elapsedSeconds)
    {
        if (_pointerHoldFocusIndex < 0)
            return;

        if (!pointer.IsPrimaryDown)
        {
            ClearPointerHold();
            return;
        }

        _pointerRepeatTimer -= elapsedSeconds;
        if (_pointerRepeatTimer > 0f)
            return;

        _pointerRepeatTimer = HeldCommandRepeat.DefaultIntervalSeconds;
        _settingsFocusIndex = _pointerHoldFocusIndex;
        GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
        _settingsEntries[_pointerHoldFocusIndex].Activate();
    }

    private void ClearPointerHold()
    {
        _pointerHoldFocusIndex = -1;
        _pointerRepeatTimer = 0f;
    }

    private bool TryFindStepperUnderPointer(out int focusIndex)
    {
        focusIndex = -1;
        var over = GumService.Default.Cursor.FrameworkElementOver;
        if (over is null)
            return false;

        Button?[] steppers =
        [
            _minDecrease, _minIncrease,
            _maxDecrease, _maxIncrease,
            _slotsDecrease, _slotsIncrease,
            _goldDecrease, _goldIncrease,
            _capDecrease, _capIncrease,
        ];

        foreach (var stepper in steppers)
        {
            if (stepper is null)
                continue;
            if (!ReferenceEquals(over, stepper)
                && !ReferenceEquals(over, stepper.Visual))
            {
                continue;
            }

            for (var i = 0; i < _settingsEntries.Count; i++)
            {
                if (!ReferenceEquals(_settingsEntries[i].Button, stepper))
                    continue;
                focusIndex = i;
                return true;
            }
        }

        return false;
    }
}
