using Gum;
using Gum.Forms.Controls;
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

        var built = EditorTwoColumnFormShell.Build(
            "New Scenario Module",
            "Left: Id/Title (click to type). Right: Create / Back. Left/Right (or LB/RB) switches columns.",
            maxShellWidthPixels: 720f,
            settingsWidthPercent: 58f,
            menuWidthPercent: 40f,
            settingsListMinHeight: 180f);
        _rootPanel = built.RootPanel;

        EditorTwoColumnFormShell.AddTextField(built.SettingsHost, "Id (folder name)", defaultModuleId, out _idBox);
        EditorTwoColumnFormShell.AddTextField(built.SettingsHost, "Title (display name)", defaultTitle, out _titleBox);

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        built.SettingsHost.AddChild(_statusLabel);

        EditorTwoColumnFormShell.AddMenuButton(
            built.MenuHost,
            _menuEntries,
            "Create",
            onCreate,
            onFocused: () => SetFocusZone(FocusZone.Menu, resetIndex: false));
        EditorTwoColumnFormShell.AddMenuButton(
            built.MenuHost,
            _menuEntries,
            "Back",
            onBack,
            onFocused: () => SetFocusZone(FocusZone.Menu, resetIndex: false));

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

    private bool TrySwitchColumn(IGameCommandSource commands)
    {
        // Always switch (even when already on target) so Settings clears menu focus for TextBox typing.
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
}
