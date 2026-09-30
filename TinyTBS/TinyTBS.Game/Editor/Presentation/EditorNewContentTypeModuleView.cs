using Gum;
using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>New units or buildings module: settings (left) + Create/Back menu (right).</summary>
public sealed class EditorNewContentTypeModuleView
{
    private readonly EditorTwoColumnFormController _form = new(allowDpadColumnSwitch: true);

    private Label? _statusLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private ContentModuleType _moduleType = ContentModuleType.Units;

    public string ModuleId
    {
        get
        {
            var fallback = _moduleType switch
            {
                ContentModuleType.Units => "user_units",
                ContentModuleType.Buildings => "user_buildings",
                ContentModuleType.Theme => "user_theme",
                _ => "user_module",
            };
            return string.IsNullOrWhiteSpace(_idBox?.Text) ? fallback : _idBox!.Text;
        }
    }

    public string ModuleTitle => _titleBox?.Text ?? ModuleId;

    public bool IsTextEntryActive => EditorGumTextEntry.IsAnyFocused(_idBox, _titleBox);

    public void Build(
        ContentModuleType moduleType,
        string defaultModuleId,
        string defaultTitle,
        Action onCreate,
        Action onBack)
    {
        Clear();
        _moduleType = moduleType;

        var titleText = moduleType switch
        {
            ContentModuleType.Units => "New Units Module",
            ContentModuleType.Buildings => "New Buildings Module",
            ContentModuleType.Theme => "New Theme Module",
            _ => "New Module",
        };

        var built = EditorTwoColumnFormShell.Build(
            titleText,
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
