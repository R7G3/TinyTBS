using Gum;
using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>
/// New map size: Id/Title, presets and width/height steppers on the left; Create/Back on the right.
/// </summary>
public sealed class EditorNewMapView
{
    private const int MinSize = 4;
    private const int MaxSize = 64;

    private static readonly (int Width, int Height)[] Presets =
    [
        (10, 8),
        (12, 12),
        (16, 16),
        (20, 16),
    ];

    private readonly EditorTwoColumnFormController _form = new(allowDpadColumnSwitch: false);

    private Label? _widthLabel;
    private Label? _heightLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private int _width = 10;
    private int _height = 8;

    public int Width => _width;

    public int Height => _height;

    public string MapId =>
        string.IsNullOrWhiteSpace(_idBox?.Text) ? "map" : _idBox!.Text.Trim();

    public string MapTitle =>
        string.IsNullOrWhiteSpace(_titleBox?.Text) ? MapId : _titleBox!.Text.Trim();

    /// <summary>True while a TextBox owns keyboard focus (Backspace must edit text, not Cancel).</summary>
    public bool IsTextEntryActive => EditorGumTextEntry.IsAnyFocused(_idBox, _titleBox);

    public void Build(
        string moduleId,
        string defaultMapId,
        Action onCreate,
        Action onBack)
    {
        Clear();
        _width = 10;
        _height = 8;

        var built = EditorTwoColumnFormShell.Build(
            "New Map — " + moduleId,
            "Click Id/Title to type. Up/Down: presets and size. Left/Right adjusts the focused size. LB/RB: Create / Back.",
            maxShellWidthPixels: 720f,
            settingsListMinHeight: 280f);
        _form.Attach(built);

        EditorTwoColumnFormShell.AddTextField(built.SettingsHost, "Id (folder name)", defaultMapId, out _idBox);
        EditorTwoColumnFormShell.AddTextField(built.SettingsHost, "Title (display name)", defaultMapId, out _titleBox);

        foreach (var preset in Presets)
        {
            var captured = preset;
            _form.AddSettingsButton(
                $"{captured.Width}×{captured.Height}",
                () => ApplyPreset(captured.Width, captured.Height));
        }

        _widthLabel = AddCaption(built.SettingsHost, WidthCaption());
        _form.AddStepper(() => AdjustWidth(-1), () => AdjustWidth(1));

        _heightLabel = AddCaption(built.SettingsHost, HeightCaption());
        _form.AddStepper(() => AdjustHeight(-1), () => AdjustHeight(1));

        _form.AddMenuButton(built.MenuHost, "Create", onCreate);
        _form.AddMenuButton(built.MenuHost, "Back", onBack);
        _form.FocusSettings(resetIndex: true);
    }

    public void HandleInput(IGameCommandSource commands, IPointerSource pointer, float elapsedSeconds)
    {
        if (IsTextEntryActive)
            return;

        _form.HandleInput(commands, elapsedSeconds, pointer);
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _form.Clear();
        _widthLabel = null;
        _heightLabel = null;
        _idBox = null;
        _titleBox = null;
    }

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

    private static Label AddCaption(Panel parent, string text)
    {
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        parent.AddChild(label);
        return label;
    }
}
