using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>
/// New map size: Id/Title fields + preset row + width/height ± (Confirm / mouse hold-repeat like Lobby).
/// </summary>
public sealed class EditorNewMapView
{
    private const int MinSize = 4;
    private const int MaxSize = 64;
    private const float TextFieldHeight = 40f;

    private static readonly (int Width, int Height)[] Presets =
    [
        (10, 8),
        (12, 12),
        (16, 16),
        (20, 16),
    ];

    private Panel? _rootPanel;
    private Label? _widthLabel;
    private Label? _heightLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private Button? _widthDecrease;
    private Button? _widthIncrease;
    private Button? _heightDecrease;
    private Button? _heightIncrease;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;
    private int _width = 10;
    private int _height = 8;
    private float _confirmRepeatTimer;
    private float _pointerRepeatTimer;
    private int _pointerHoldFocusIndex = -1;

    public int Width => _width;

    public int Height => _height;

    public string MapId =>
        string.IsNullOrWhiteSpace(_idBox?.Text) ? "map" : _idBox!.Text.Trim();

    public string MapTitle =>
        string.IsNullOrWhiteSpace(_titleBox?.Text) ? MapId : _titleBox!.Text.Trim();

    /// <summary>True while a TextBox owns keyboard focus (Backspace must edit text, not Cancel).</summary>
    public bool IsTextEntryActive => IsTextFieldFocused();

    public void Build(
        string moduleId,
        string defaultMapId,
        Action onCreate,
        Action onBack)
    {
        Clear();
        _width = 10;
        _height = 8;

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var body = new Panel();
        body.Dock(Dock.Fill);
        _rootPanel.AddChild(body);

        var shell = new Panel();
        // Top-aligned: Id/Title stay on-screen (center + tall content clipped the top).
        GumUiLayout.CenterHorizontallyInParent(shell);
        shell.Visual.Y = 16f;
        GumUiLayout.SetBoundedWidth(shell, maxPixels: 560f, parentPercent: 94f);
        var canvasHeight = Math.Max(320f, GumService.Default.CanvasHeight);
        GumUiLayout.SetBoundedHeight(shell, maxPixels: canvasHeight - 32f, parentPercent: 94f);
        body.AddChild(shell);
        GumUiLayout.AddSolidBackground(shell, UiColors.EditorPanel);

        var scroll = new ScrollViewer();
        scroll.Dock(Dock.Fill);
        scroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        scroll.InnerPanel.Width = 0;
        scroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        GumScrollViewerChrome.DisableScrollChromeFocus(scroll);
        shell.AddChild(scroll);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 8f, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        scroll.AddChild(stack);

        GumUiLayout.AddVerticalSpacer(stack, 12f);

        var title = new Label { Text = "New Map — " + moduleId };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        var hint = new Label { Text = "Set Id/Title, then size. Click a field to type." };
        GumUiLayout.FillParentWidth(hint);
        stack.AddChild(hint);

        AddTextField(stack, "Id (folder name)", defaultMapId, out _idBox);
        AddTextField(stack, "Title (display name)", defaultMapId, out _titleBox);

        var presetHost = new Panel();
        GumUiLayout.FillParentWidth(presetHost);
        presetHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.AddChild(presetHost);

        var presetButtons = new List<Button>();
        foreach (var preset in Presets)
        {
            var captured = preset;
            var button = new Button { Text = $"{captured.Width}×{captured.Height}" };
            button.Click += (_, _) => ApplyPreset(captured.Width, captured.Height);
            presetButtons.Add(button);
            _focusableEntries.Add((button, () => ApplyPreset(captured.Width, captured.Height)));
        }

        GumUiLayout.LayoutAdaptiveButtonRows(presetHost, presetButtons, availableWidth: 500f, spacing: 8f);

        _widthLabel = new Label { Text = WidthCaption() };
        GumUiLayout.FillParentWidth(_widthLabel);
        stack.AddChild(_widthLabel);
        AddStepperRow(stack, onDecrease: () => AdjustWidth(-1), onIncrease: () => AdjustWidth(1), out _widthDecrease, out _widthIncrease);

        _heightLabel = new Label { Text = HeightCaption() };
        GumUiLayout.FillParentWidth(_heightLabel);
        stack.AddChild(_heightLabel);
        AddStepperRow(stack, onDecrease: () => AdjustHeight(-1), onIncrease: () => AdjustHeight(1), out _heightDecrease, out _heightIncrease);

        AddPlainButton(stack, "Create", onCreate);
        AddPlainButton(stack, "Back", onBack);

        GumUiLayout.AddVerticalSpacer(stack, 14f);
        FocusFirst();
    }

    public void HandleInput(IGameCommandSource commands, IPointerSource pointer, float elapsedSeconds)
    {
        // Let Gum own keyboard while a text field is focused.
        if (IsTextFieldFocused())
            return;

        TickPointerHold(pointer, elapsedSeconds);
        TryBeginPointerHold(pointer);

        if (IsStepperFocused()
            && HeldCommandRepeat.TryTick(commands, GameCommand.Confirm, elapsedSeconds, ref _confirmRepeatTimer))
        {
            _focusableEntries[_focusIndex].Activate();
            return;
        }

        if (!IsStepperFocused())
            _confirmRepeatTimer = 0f;

        // Left/Right also move focus so preset row is usable with pad.
        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _focusableEntries,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds,
            mapHorizontalToVertical: true);
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _widthLabel = null;
        _heightLabel = null;
        _idBox = null;
        _titleBox = null;
        _widthDecrease = null;
        _widthIncrease = null;
        _heightDecrease = null;
        _heightIncrease = null;
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;
        _confirmRepeatTimer = 0f;
        ClearPointerHold();
    }

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

    private bool IsTextFieldFocused() =>
        _idBox is { IsFocused: true } || _titleBox is { IsFocused: true };

    private void ApplyPreset(int width, int height)
    {
        _width = Math.Clamp(width, MinSize, MaxSize);
        _height = Math.Clamp(height, MinSize, MaxSize);
        SyncLabels();
    }

    private void AdjustWidth(int delta)
    {
        _width = Math.Clamp(_width + delta, MinSize, MaxSize);
        SyncLabels();
    }

    private void AdjustHeight(int delta)
    {
        _height = Math.Clamp(_height + delta, MinSize, MaxSize);
        SyncLabels();
    }

    private void SyncLabels()
    {
        if (_widthLabel is not null)
            _widthLabel.Text = WidthCaption();
        if (_heightLabel is not null)
            _heightLabel.Text = HeightCaption();
    }

    private string WidthCaption() => $"Width: {_width}";

    private string HeightCaption() => $"Height: {_height}";

    private void FocusFirst()
    {
        _focusIndex = 0;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
    }

    private void AddStepperRow(
        Panel parent,
        Action onDecrease,
        Action onIncrease,
        out Button decreaseButton,
        out Button increaseButton)
    {
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
        // No Gum Click — press/hold owned by HandleInput (Lobby pattern).
        var button = new Button { Text = text };
        GumUiLayout.SetAbsoluteWidth(button, 72f);
        row.AddChild(button);
        _focusableEntries.Add((button, onActivate));
        return button;
    }

    private void AddPlainButton(Panel parent, string text, Action onClick)
    {
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) => onClick();
        parent.AddChild(button);
        _focusableEntries.Add((button, onClick));
    }

    private bool IsStepperFocused()
    {
        if (_focusIndex < 0 || _focusIndex >= _focusableEntries.Count)
            return false;
        var button = _focusableEntries[_focusIndex].Button;
        return ReferenceEquals(button, _widthDecrease)
            || ReferenceEquals(button, _widthIncrease)
            || ReferenceEquals(button, _heightDecrease)
            || ReferenceEquals(button, _heightIncrease);
    }

    private void TryBeginPointerHold(IPointerSource pointer)
    {
        if (!pointer.WasPrimaryPressed)
            return;

        if (!TryFindStepperUnderPointer(out var focusIndex))
            return;

        _focusIndex = focusIndex;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
        _focusableEntries[focusIndex].Activate();
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
        _focusIndex = _pointerHoldFocusIndex;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
        _focusableEntries[_pointerHoldFocusIndex].Activate();
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

        var steppers = new Button?[] { _widthDecrease, _widthIncrease, _heightDecrease, _heightIncrease };
        foreach (var stepper in steppers)
        {
            if (stepper is null)
                continue;
            if (!ReferenceEquals(over, stepper)
                && !ReferenceEquals(over, stepper.Visual))
            {
                continue;
            }

            for (var i = 0; i < _focusableEntries.Count; i++)
            {
                if (!ReferenceEquals(_focusableEntries[i].Button, stepper))
                    continue;
                focusIndex = i;
                return true;
            }
        }

        return false;
    }
}
