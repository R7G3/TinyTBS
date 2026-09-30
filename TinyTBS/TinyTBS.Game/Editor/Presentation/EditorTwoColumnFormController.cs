using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>
/// Focus and input of an <see cref="EditorTwoColumnFormShell"/> form: a scrolling settings column of buttons and
/// −/+ stepper rows (left) and a menu column (right). LB/RB switch columns (Left/Right too when the form has no
/// steppers); Left/Right move within a stepper row; holding Confirm or the mouse button repeats a stepper.
/// </summary>
public sealed class EditorTwoColumnFormController
{
    private const float StepperButtonWidth = 72f;
    private const float StepperRowSpacing = 8f;

    private readonly bool _allowDpadColumnSwitch;
    private readonly List<(Button Button, Action Activate)> _settingsEntries = [];
    private readonly List<(Button Button, Action Activate)> _menuEntries = [];
    private readonly List<(Button Decrease, Button Increase)> _stepperRows = [];
    private readonly HashSet<Button> _stepperButtons = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();

    private ScrollViewer? _settingsScroll;
    private Panel? _settingsHost;
    private bool _isMenuFocused;
    private int _settingsFocusIndex;
    private int _menuFocusIndex;
    private float _confirmRepeatTimer;
    private float _pointerRepeatTimer;
    private int _pointerHoldFocusIndex = -1;

    /// <param name="allowDpadColumnSwitch">
    /// True for forms without steppers: D-pad Left/Right then switch columns like LB/RB.
    /// </param>
    public EditorTwoColumnFormController(bool allowDpadColumnSwitch)
    {
        _allowDpadColumnSwitch = allowDpadColumnSwitch;
    }

    /// <summary>Shared with detail overlays so held D-pad repeat timing carries over.</summary>
    public MenuVerticalNavigateRepeat NavigateRepeat => _navigateRepeat;

    public void Attach(EditorTwoColumnFormShell.BuiltShell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        _settingsScroll = shell.SettingsScroll;
        _settingsHost = shell.SettingsHost;
    }

    public void AddMenuButton(Panel menuHost, string text, Action onClick) =>
        EditorTwoColumnFormShell.AddMenuButton(
            menuHost,
            _menuEntries,
            text,
            onClick,
            onFocused: () => FocusMenu(resetIndex: false));

    /// <summary>
    /// Clears the settings column, lets <paramref name="addRows"/> add it again and restores the focused row.
    /// </summary>
    public void RebuildSettings(Action addRows)
    {
        ArgumentNullException.ThrowIfNull(addRows);

        var previousFocus = _settingsFocusIndex;
        _settingsEntries.Clear();
        _stepperRows.Clear();
        _stepperButtons.Clear();
        _settingsHost?.Visual.Children.Clear();

        addRows();

        if (_settingsEntries.Count == 0)
            return;

        _settingsFocusIndex = Math.Clamp(previousFocus, 0, _settingsEntries.Count - 1);
        if (_isMenuFocused)
            return;

        GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
        EnsureSettingsRowVisible();
    }

    public Button AddSettingsButton(string text, Action onClick)
    {
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        RequireSettingsHost().AddChild(button);
        _settingsEntries.Add((button, onClick));
        button.Click += (_, _) =>
        {
            FocusSettingsButton(button);
            onClick();
        };
        return button;
    }

    /// <summary>A − / + row; the buttons fire on press and repeat while held (pointer or Confirm).</summary>
    public void AddStepper(Action onDecrease, Action onIncrease)
    {
        var row = new Panel();
        GumUiLayout.FillParentWidth(row);
        row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.Visual.StackSpacing = StepperRowSpacing;
        RequireSettingsHost().AddChild(row);

        var decrease = AddStepperButton(row, "-", onDecrease);
        var increase = AddStepperButton(row, "+", onIncrease);
        _stepperRows.Add((decrease, increase));
    }

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds, IPointerSource? pointer = null)
    {
        if (pointer is not null)
        {
            TickPointerHold(pointer, elapsedSeconds);
            TryBeginPointerHold(pointer);
        }

        if (TrySwitchColumn(commands))
            return;

        if (_isMenuFocused)
            HandleMenuInput(commands, elapsedSeconds);
        else
            HandleSettingsInput(commands, elapsedSeconds);
    }

    public void FocusSettings(bool resetIndex = false)
    {
        _isMenuFocused = false;
        _settingsFocusIndex = resetIndex || _settingsEntries.Count == 0
            ? 0
            : Math.Clamp(_settingsFocusIndex, 0, _settingsEntries.Count - 1);

        GumFocusableButtonList.ClearFocus(_menuEntries);
        if (_settingsEntries.Count == 0)
            return;

        GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
        EnsureSettingsRowVisible();
    }

    public void FocusMenu(bool resetIndex = false)
    {
        _isMenuFocused = true;
        _menuFocusIndex = resetIndex || _menuEntries.Count == 0
            ? 0
            : Math.Clamp(_menuFocusIndex, 0, _menuEntries.Count - 1);

        GumFocusableButtonList.ClearFocus(_settingsEntries);
        if (_menuEntries.Count > 0)
            GumFocusableButtonList.ApplyFocus(_menuEntries, ref _menuFocusIndex);
    }

    public void Clear()
    {
        _settingsScroll = null;
        _settingsHost = null;
        _settingsEntries.Clear();
        _menuEntries.Clear();
        _stepperRows.Clear();
        _stepperButtons.Clear();
        _navigateRepeat.Reset();
        _isMenuFocused = false;
        _settingsFocusIndex = 0;
        _menuFocusIndex = 0;
        _confirmRepeatTimer = 0f;
        ClearPointerHold();
    }

    private Panel RequireSettingsHost() =>
        _settingsHost ?? throw new InvalidOperationException("Attach the form shell before adding settings rows.");

    private Button AddStepperButton(Panel row, string text, Action onActivate)
    {
        var button = new Button { Text = text };
        GumUiLayout.SetAbsoluteWidth(button, StepperButtonWidth);
        row.AddChild(button);
        _settingsEntries.Add((button, onActivate));
        _stepperButtons.Add(button);
        button.Click += (_, _) => FocusSettingsButton(button);
        return button;
    }

    private void FocusSettingsButton(Button button)
    {
        var index = IndexOfSettingsButton(button);
        if (index >= 0)
            _settingsFocusIndex = index;

        FocusSettings(resetIndex: false);
    }

    private bool TrySwitchColumn(IGameCommandSource commands)
    {
        var toMenu = commands.WasPressed(GameCommand.FocusNextRegion)
            || commands.WasPressed(GameCommand.ZoomIn)
            || (_allowDpadColumnSwitch && commands.WasPressed(GameCommand.NavigateRight));
        var toSettings = commands.WasPressed(GameCommand.FocusPreviousRegion)
            || commands.WasPressed(GameCommand.ZoomOut)
            || (_allowDpadColumnSwitch && commands.WasPressed(GameCommand.NavigateLeft));

        if (toMenu && !_isMenuFocused)
        {
            FocusMenu();
            return true;
        }

        if (toSettings && _isMenuFocused)
        {
            FocusSettings();
            return true;
        }

        return toMenu || toSettings;
    }

    private void HandleMenuInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_menuEntries.Count > 0)
            GumFocusableButtonList.HandleVerticalInput(commands, _menuEntries, ref _menuFocusIndex, _navigateRepeat, elapsedSeconds);
    }

    private void HandleSettingsInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_settingsEntries.Count == 0)
            return;

        if (!IsStepperFocused())
        {
            _confirmRepeatTimer = 0f;
            var result = GumFocusableButtonList.HandleVerticalInput(
                commands,
                _settingsEntries,
                ref _settingsFocusIndex,
                _navigateRepeat,
                elapsedSeconds);
            if (result == GumFocusListResult.Navigated)
                EnsureSettingsRowVisible();
            return;
        }

        if (HeldCommandRepeat.TryTick(commands, GameCommand.Confirm, elapsedSeconds, ref _confirmRepeatTimer))
        {
            _settingsEntries[_settingsFocusIndex].Activate();
            return;
        }

        var horizontal = _navigateRepeat.TryGetHorizontalDelta(commands, elapsedSeconds);
        if (horizontal != 0)
        {
            MoveWithinStepperRow(horizontal);
            EnsureSettingsRowVisible();
            return;
        }

        var vertical = _navigateRepeat.TryGetDelta(commands, elapsedSeconds);
        if (vertical != 0)
        {
            MoveFromStepperVertically(vertical);
            EnsureSettingsRowVisible();
            return;
        }

        GumFocusableButtonList.MaintainFocus(_settingsEntries, ref _settingsFocusIndex);
    }

    private bool IsStepperFocused() =>
        !_isMenuFocused
        && _settingsFocusIndex >= 0
        && _settingsFocusIndex < _settingsEntries.Count
        && _stepperButtons.Contains(_settingsEntries[_settingsFocusIndex].Button);

    private void MoveWithinStepperRow(int horizontalDelta)
    {
        if (TryGetStepperPlacement(out var rowIndex, out var onIncrease) && onIncrease != horizontalDelta > 0)
            FocusStepper(rowIndex, horizontalDelta > 0);
    }

    /// <summary>Up/Down skips the partner button of the row; landing on another stepper row keeps the − / + side.</summary>
    private void MoveFromStepperVertically(int verticalDelta)
    {
        if (!TryGetStepperPlacement(out var rowIndex, out var onIncrease))
            return;

        var (decrease, increase) = _stepperRows[rowIndex];
        var targetIndex = verticalDelta < 0
            ? IndexOfSettingsButton(decrease) - 1
            : IndexOfSettingsButton(increase) + 1;
        if (targetIndex < 0 || targetIndex >= _settingsEntries.Count)
            return;

        var adjacentRow = rowIndex + Math.Sign(verticalDelta);
        if (adjacentRow >= 0 && adjacentRow < _stepperRows.Count)
        {
            var target = _settingsEntries[targetIndex].Button;
            var adjacent = _stepperRows[adjacentRow];
            if (ReferenceEquals(target, adjacent.Decrease) || ReferenceEquals(target, adjacent.Increase))
            {
                FocusStepper(adjacentRow, onIncrease);
                return;
            }
        }

        _settingsFocusIndex = targetIndex;
        GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
    }

    private bool TryGetStepperPlacement(out int rowIndex, out bool onIncrease)
    {
        rowIndex = -1;
        onIncrease = false;
        if (_settingsFocusIndex < 0 || _settingsFocusIndex >= _settingsEntries.Count)
            return false;

        var button = _settingsEntries[_settingsFocusIndex].Button;
        for (var i = 0; i < _stepperRows.Count; i++)
        {
            if (ReferenceEquals(button, _stepperRows[i].Decrease) || ReferenceEquals(button, _stepperRows[i].Increase))
            {
                rowIndex = i;
                onIncrease = ReferenceEquals(button, _stepperRows[i].Increase);
                return true;
            }
        }

        return false;
    }

    private void FocusStepper(int rowIndex, bool onIncrease)
    {
        var target = onIncrease ? _stepperRows[rowIndex].Increase : _stepperRows[rowIndex].Decrease;
        var index = IndexOfSettingsButton(target);
        if (index < 0)
            return;

        _settingsFocusIndex = index;
        GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
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

    private void EnsureSettingsRowVisible()
    {
        if (_settingsEntries.Count == 0)
            return;

        EditorFormScrollFocus.AfterNavigate(
            _settingsScroll,
            _settingsHost,
            _settingsEntries,
            listFocusStartIndex: 0,
            listFocusCount: _settingsEntries.Count,
            _settingsFocusIndex,
            EditorTwoColumnFormShell.DefaultStackSpacing,
            EditorTwoColumnFormShell.DefaultMinScrollViewport);
    }

    private void TryBeginPointerHold(IPointerSource pointer)
    {
        if (!pointer.WasPrimaryPressed || !TryFindStepperUnderPointer(out var focusIndex))
            return;

        _settingsFocusIndex = focusIndex;
        FocusSettings(resetIndex: false);
        _settingsEntries[focusIndex].Activate();
        _pointerHoldFocusIndex = focusIndex;
        _pointerRepeatTimer = HeldCommandRepeat.DefaultInitialDelaySeconds;
    }

    private void TickPointerHold(IPointerSource pointer, float elapsedSeconds)
    {
        if (_pointerHoldFocusIndex < 0)
            return;

        if (!pointer.IsPrimaryDown || _pointerHoldFocusIndex >= _settingsEntries.Count)
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

        foreach (var stepper in _stepperButtons)
        {
            if (!ReferenceEquals(over, stepper) && !ReferenceEquals(over, stepper.Visual))
                continue;

            focusIndex = IndexOfSettingsButton(stepper);
            return focusIndex >= 0;
        }

        return false;
    }
}
