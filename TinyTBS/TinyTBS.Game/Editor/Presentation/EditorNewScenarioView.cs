using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>New scenario module: settings (left) + Create/Back menu (right).</summary>
public sealed class EditorNewScenarioView
{
    private enum FocusZone
    {
        Settings = 0,
        Menu = 1,
    }

    private const float TextFieldHeight = 40f;
    private const float StackSpacing = 6f;
    private const float ColumnMinHeight = 180f;

    private Panel? _rootPanel;
    private Label? _statusLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private readonly List<(Button Button, Action Activate)> _menuEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private FocusZone _focusZone = FocusZone.Menu;
    private int _menuFocusIndex;

    public string ModuleId =>
        string.IsNullOrWhiteSpace(_idBox?.Text) ? "user_scenario" : _idBox!.Text.Trim();

    public string ModuleTitle =>
        string.IsNullOrWhiteSpace(_titleBox?.Text) ? ModuleId : _titleBox!.Text.Trim();

    /// <summary>True while a TextBox owns keyboard focus (Backspace must edit text, not Cancel).</summary>
    public bool IsTextEntryActive =>
        _idBox is { IsFocused: true } || _titleBox is { IsFocused: true };

    public void Build(string defaultModuleId, string defaultTitle, Action onCreate, Action onBack)
    {
        Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var body = new Panel();
        body.Dock(Dock.Fill);
        _rootPanel.AddChild(body);

        var shell = new Panel();
        GumUiLayout.CenterHorizontallyInParent(shell);
        shell.Visual.Y = 12f;
        GumUiLayout.SetBoundedWidth(shell, maxPixels: 720f, parentPercent: 94f);
        var canvasHeight = Math.Max(320f, GumService.Default.CanvasHeight);
        var shellHeight = Math.Max(300f, Math.Min(canvasHeight - 24f, 420f));
        const float topChrome = 10f + 26f + 22f + StackSpacing * 3f;
        const float bottomChrome = 10f;
        const float columnHeader = 26f + 4f;
        var columnHeight = Math.Max(ColumnMinHeight, shellHeight - topChrome - bottomChrome - columnHeader);
        GumUiLayout.SetAbsoluteHeight(shell, topChrome + bottomChrome + columnHeader + columnHeight);
        body.AddChild(shell);
        GumUiLayout.AddSolidBackground(shell, EditorUiColors.Panel);

        var rootStack = GumUiLayout.CreateVerticalStackPanel(spacing: StackSpacing, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(rootStack);
        shell.AddChild(rootStack);
        GumUiLayout.AddVerticalSpacer(rootStack, 10f);

        var title = new Label { Text = "New Scenario Module" };
        GumUiLayout.FillParentWidth(title);
        rootStack.AddChild(title);

        var hint = new Label
        {
            Text = "Left: Id/Title (click to type). Right: Create / Back. Left/Right (or LB/RB) switches columns.",
        };
        GumUiLayout.FillParentWidth(hint);
        rootStack.AddChild(hint);

        var columns = new Panel();
        GumUiLayout.FillParentWidth(columns);
        columns.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        columns.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        columns.Visual.StackSpacing = 10f;
        rootStack.AddChild(columns);

        var left = CreateSettingsColumn(columns, columnHeight, defaultModuleId, defaultTitle);
        var right = CreateMenuColumn(columns, columnHeight, onCreate, onBack);
        GumUiLayout.SetWidthPercent(left, 58f);
        GumUiLayout.SetWidthPercent(right, 40f);

        GumUiLayout.AddVerticalSpacer(rootStack, 10f);
        SetFocusZone(FocusZone.Menu, resetIndex: true);
    }

    public void SyncStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text ?? string.Empty;
    }

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (IsTextEntryActive)
            return;

        if (TrySwitchColumn(commands))
            return;

        // Settings column has no gamepad buttons — Create/Back are vertical only.
        if (_focusZone != FocusZone.Menu || _menuEntries.Count == 0)
            return;

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _menuEntries,
            ref _menuFocusIndex,
            _navigateRepeat,
            elapsedSeconds);
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _statusLabel = null;
        _idBox = null;
        _titleBox = null;
        _menuEntries.Clear();
        _navigateRepeat.Reset();
        _focusZone = FocusZone.Menu;
        _menuFocusIndex = 0;
    }

    private Panel CreateSettingsColumn(
        Panel columns,
        float columnHeight,
        string defaultModuleId,
        string defaultTitle)
    {
        var column = new Panel();
        column.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        column.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        column.Visual.StackSpacing = 4f;
        columns.AddChild(column);

        var header = new Label { Text = "Settings" };
        GumUiLayout.FillParentWidth(header);
        column.AddChild(header);

        var host = new Panel();
        GumUiLayout.FillParentWidth(host);
        host.Visual.Height = columnHeight;
        host.Visual.HeightUnits = DimensionUnitType.Absolute;
        host.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        host.Visual.StackSpacing = StackSpacing;
        column.AddChild(host);

        AddTextField(host, "Id (folder name)", defaultModuleId, out _idBox);
        AddTextField(host, "Title (display name)", defaultTitle, out _titleBox);

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        host.AddChild(_statusLabel);
        return column;
    }

    private Panel CreateMenuColumn(Panel columns, float columnHeight, Action onCreate, Action onBack)
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
        host.Visual.Height = columnHeight;
        host.Visual.HeightUnits = DimensionUnitType.Absolute;
        host.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        host.Visual.StackSpacing = 6f;
        column.AddChild(host);

        AddMenuButton(host, "Create", onCreate);
        AddMenuButton(host, "Back", onBack);
        return column;
    }

    private bool TrySwitchColumn(IGameCommandSource commands)
    {
        // No horizontal rows in columns — Left/Right switch Settings ↔ Menu (plus LB/RB).
        var toMenu = commands.WasPressed(GameCommand.NavigateRight)
            || commands.WasPressed(GameCommand.FocusNextRegion)
            || commands.WasPressed(GameCommand.ZoomIn);
        var toSettings = commands.WasPressed(GameCommand.NavigateLeft)
            || commands.WasPressed(GameCommand.FocusPreviousRegion)
            || commands.WasPressed(GameCommand.ZoomOut);

        if (toMenu)
        {
            SetFocusZone(FocusZone.Menu, resetIndex: false);
            return true;
        }

        if (toSettings)
        {
            // No gamepad targets in Settings — clear menu focus so mouse can use text fields.
            SetFocusZone(FocusZone.Settings, resetIndex: false);
            return true;
        }

        return false;
    }

    private void SetFocusZone(FocusZone zone, bool resetIndex)
    {
        _focusZone = zone;
        if (zone == FocusZone.Menu)
        {
            if (resetIndex)
                _menuFocusIndex = 0;
            else
                _menuFocusIndex = Math.Clamp(_menuFocusIndex, 0, Math.Max(0, _menuEntries.Count - 1));

            if (_menuEntries.Count > 0)
                GumFocusableButtonList.ApplyFocus(_menuEntries, ref _menuFocusIndex);
            return;
        }

        // Settings: release gamepad button focus so TextBox can take keyboard.
        GumFocusableButtonList.ClearFocus(_menuEntries);
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
}
