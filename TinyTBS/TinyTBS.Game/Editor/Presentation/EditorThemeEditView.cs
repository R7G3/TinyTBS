using Gum;
using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Editor.Themes;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Edit theme module.json: settings scroll (left) + Save/Back menu (right).</summary>
public sealed class EditorThemeEditView
{
    private readonly EditorTwoColumnFormController _form = new(allowDpadColumnSwitch: true);

    private Panel? _rootPanel;
    private Panel? _settingsHost;
    private Label? _statusLabel;
    private TextBox? _titleBox;
    private TextBox? _descriptionBox;
    private TextBox? _versionBox;
    private TextBox? _terrainDirBox;
    private TextBox? _gravestoneBox;
    private TextBox? _newContentIdBox;
    private TextBox? _newBaseBox;
    private TextBox? _newMaskBox;
    private readonly EditorChoiceOverlay _choiceOverlay = new();

    private EditableThemeDocument _document = new();

    public bool IsTextEntryActive =>
        EditorGumTextEntry.IsAnyFocused(
            _titleBox,
            _descriptionBox,
            _versionBox,
            _terrainDirBox,
            _gravestoneBox,
            _newContentIdBox,
            _newBaseBox,
            _newMaskBox);

    public bool IsOverlayOpen => _choiceOverlay.IsOpen;

    public EditableThemeDocument Document => _document;

    public void Build(EditableThemeDocument document, Action onSave, Action onBack)
    {
        Clear();
        _document = document ?? throw new ArgumentNullException(nameof(document));

        var built = EditorTwoColumnFormShell.Build(
            "Edit Theme — " + _document.ModuleId,
            "LB/RB: columns. Up/Down: rows. Remap ids use namespace/localId (e.g. "
            + VanillaContentIds.ContentNamespace
            + "/king).");
        _rootPanel = built.RootPanel;
        _settingsHost = built.SettingsHost;
        _form.Attach(built);

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        built.MenuHost.AddChild(_statusLabel);

        _form.AddMenuButton(
            built.MenuHost,
            "Save theme",
            () =>
            {
                ApplyTextFields();
                onSave();
            });
        _form.AddMenuButton(built.MenuHost, "Back", onBack);

        RebuildSettingsColumn();
        _form.FocusSettings(resetIndex: true);
    }

    public void SyncStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text ?? string.Empty;
    }

    public void ApplyTextFields()
    {
        if (_titleBox is not null && !string.IsNullOrWhiteSpace(_titleBox.Text))
            _document.Title = _titleBox.Text;
        if (_descriptionBox is not null)
        {
            _document.Description = string.IsNullOrWhiteSpace(_descriptionBox.Text)
                ? null
                : _descriptionBox.Text;
        }

        if (_versionBox is not null && !string.IsNullOrWhiteSpace(_versionBox.Text))
            _document.Version = _versionBox.Text;
        if (_terrainDirBox is not null && !string.IsNullOrWhiteSpace(_terrainDirBox.Text))
            _document.TerrainDirectory = _terrainDirBox.Text;
        if (_gravestoneBox is not null && !string.IsNullOrWhiteSpace(_gravestoneBox.Text))
            _document.GravestoneRelativePath = _gravestoneBox.Text;

        TryCommitNewRemap();
        _document.IsDirty = true;
    }

    public bool TryCloseOverlay() => _choiceOverlay.TryClose();

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_choiceOverlay.IsOpen)
        {
            _choiceOverlay.HandleInput(commands, elapsedSeconds);
            return;
        }

        if (IsTextEntryActive)
            return;

        _form.HandleInput(commands, elapsedSeconds);
    }

    public void Clear()
    {
        _choiceOverlay.Close(notifyClosed: false);
        GumService.Default.Root.Children.Clear();
        _form.Clear();
        _rootPanel = null;
        _settingsHost = null;
        _statusLabel = null;
        _titleBox = null;
        _descriptionBox = null;
        _versionBox = null;
        _terrainDirBox = null;
        _gravestoneBox = null;
        _newContentIdBox = null;
        _newBaseBox = null;
        _newMaskBox = null;
    }

    private void RebuildSettingsColumn()
    {
        if (_settingsHost is null)
            return;

        ApplyTextFields();
        _form.RebuildSettings(AddSettingsRows);
    }

    private void AddSettingsRows()
    {
        if (_settingsHost is null)
            return;

        var moduleLabel = new Label
        {
            Text = "Module id: " + _document.ModuleId + "  ·  namespace: " + _document.ContentNamespace,
        };
        GumUiLayout.FillParentWidth(moduleLabel);
        _settingsHost.AddChild(moduleLabel);

        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Title", _document.Title, out _titleBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Description", _document.Description ?? string.Empty, out _descriptionBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Version", _document.Version, out _versionBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Terrain directory", _document.TerrainDirectory, out _terrainDirBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Gravestone path", _document.GravestoneRelativePath, out _gravestoneBox);

        var remapsHeader = new Label { Text = "Sprite remaps" };
        GumUiLayout.FillParentWidth(remapsHeader);
        _settingsHost.AddChild(remapsHeader);

        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Content id (namespace/localId)", string.Empty, out _newContentIdBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Remap base path", string.Empty, out _newBaseBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Remap mask path", string.Empty, out _newMaskBox);
        _form.AddSettingsButton("Add remap", CommitNewRemap);

        foreach (var remap in _document.Remaps.OrderBy(entry => entry.ContentIdFull, StringComparer.OrdinalIgnoreCase))
        {
            var captured = remap;
            var summary = captured.ContentIdFull + " → " + ShortPath(captured.BasePath);
            _form.AddSettingsButton(summary, () => OpenRemapChoice(captured));
        }

        if (_document.Remaps.Count == 0)
        {
            var empty = new Label { Text = "(No remaps — sprites use unit/building modules.)" };
            GumUiLayout.FillParentWidth(empty);
            _settingsHost.AddChild(empty);
        }
    }

    private void CommitNewRemap()
    {
        TryCommitNewRemap();
        RebuildSettingsColumn();
    }

    private void TryCommitNewRemap()
    {
        if (_newContentIdBox is null || _newBaseBox is null || _newMaskBox is null)
            return;

        var contentId = _newContentIdBox.Text ?? string.Empty;
        var basePath = _newBaseBox.Text ?? string.Empty;
        var maskPath = _newMaskBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(contentId)
            || string.IsNullOrWhiteSpace(basePath)
            || string.IsNullOrWhiteSpace(maskPath))
        {
            return;
        }

        var existing = _document.Remaps.FirstOrDefault(
            entry => string.Equals(entry.ContentIdFull, contentId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.BasePath = basePath;
            existing.MaskPath = maskPath;
        }
        else
        {
            _document.Remaps.Add(new EditableThemeRemapEntry
            {
                ContentIdFull = contentId,
                BasePath = basePath,
                MaskPath = maskPath,
            });
        }

        _newContentIdBox.Text = string.Empty;
        _newBaseBox.Text = string.Empty;
        _newMaskBox.Text = string.Empty;
        _document.IsDirty = true;
    }

    private void OpenRemapChoice(EditableThemeRemapEntry remap)
    {
        if (_rootPanel is null)
            return;

        var label = remap.ContentIdFull;
        _choiceOverlay.Open(
            _rootPanel,
            label,
            [
                ("Remove remap", () =>
                {
                    _document.Remaps.RemoveAll(
                        entry => string.Equals(entry.ContentIdFull, remap.ContentIdFull, StringComparison.OrdinalIgnoreCase));
                    _document.IsDirty = true;
                    RebuildSettingsColumn();
                }),
            ],
            onClosed: () => _form.FocusSettings());
    }

    private static string ShortPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "(empty)";
        var normalized = path.Replace('\\', '/');
        return normalized.Length <= 42 ? normalized : "…" + normalized[^39..];
    }
}
