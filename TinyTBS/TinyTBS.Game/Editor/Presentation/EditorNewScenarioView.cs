using Gum;
using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>New scenario module: settings (left) + Create/Back menu (right).</summary>
public sealed class EditorNewScenarioView
{
    private readonly EditorTwoColumnFormController _form = new(allowDpadColumnSwitch: true);

    private Label? _statusLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;

    public string ModuleId =>
        string.IsNullOrWhiteSpace(_idBox?.Text) ? "user_scenario" : _idBox!.Text.Trim();

    public string ModuleTitle =>
        string.IsNullOrWhiteSpace(_titleBox?.Text) ? ModuleId : _titleBox!.Text.Trim();

    /// <summary>True while a TextBox owns keyboard focus (Backspace must edit text, not Cancel).</summary>
    public bool IsTextEntryActive => EditorGumTextEntry.IsAnyFocused(_idBox, _titleBox);

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
        _form.Attach(built);

        EditorTwoColumnFormShell.AddTextField(built.SettingsHost, "Id (folder name)", defaultModuleId, out _idBox);
        EditorTwoColumnFormShell.AddTextField(built.SettingsHost, "Title (display name)", defaultTitle, out _titleBox);

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        built.SettingsHost.AddChild(_statusLabel);

        _form.AddMenuButton(built.MenuHost, "Create", onCreate);
        _form.AddMenuButton(built.MenuHost, "Back", onBack);
        _form.FocusMenu(resetIndex: true);
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

        _form.HandleInput(commands, elapsedSeconds);
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _form.Clear();
        _statusLabel = null;
        _idBox = null;
        _titleBox = null;
    }
}
