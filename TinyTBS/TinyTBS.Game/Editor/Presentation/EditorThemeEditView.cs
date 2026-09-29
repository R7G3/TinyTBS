using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Editor.Themes;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Edit theme module.json: settings scroll (left) + Save/Back menu (right).</summary>
public sealed class EditorThemeEditView
{
    private enum FocusZone
    {
        Settings = 0,
        Menu = 1,
    }

    private const float StackSpacing = 6f;
    private const float MinScrollViewport = 120f;

    private Panel? _rootPanel;
    private ScrollViewer? _settingsScroll;
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
    private readonly List<(Button Button, Action Activate)> _settingsEntries = [];
    private readonly List<(Button Button, Action Activate)> _menuEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private readonly EditorChoiceOverlay _choiceOverlay = new();

    private FocusZone _focusZone = FocusZone.Settings;
    private int _settingsFocusIndex;
    private int _menuFocusIndex;
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
            "LB/RB: columns. Up/Down: rows. Remap ids use namespace/localId (e.g. vanilla/king).");
        _rootPanel = built.RootPanel;
        _settingsHost = built.SettingsHost;
        _settingsScroll = built.SettingsScroll;

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        built.MenuHost.AddChild(_statusLabel);

        EditorTwoColumnFormShell.AddMenuButton(
            built.MenuHost,
            _menuEntries,
            "Save theme",
            () =>
            {
                ApplyTextFields();
                onSave();
            },
            onFocused: () => SetFocusZone(FocusZone.Menu, resetIndex: false));
        EditorTwoColumnFormShell.AddMenuButton(
            built.MenuHost,
            _menuEntries,
            "Back",
            onBack,
            onFocused: () => SetFocusZone(FocusZone.Menu, resetIndex: false));

        RebuildSettingsColumn();
        SetFocusZone(FocusZone.Settings, resetIndex: true);
    }

    public void SyncStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text ?? string.Empty;
    }

    public void ApplyTextFields()
    {
        if (_titleBox is not null && !string.IsNullOrWhiteSpace(_titleBox.Text))
            _document.Title = _titleBox.Text.Trim();
        if (_descriptionBox is not null)
        {
            _document.Description = string.IsNullOrWhiteSpace(_descriptionBox.Text)
                ? null
                : _descriptionBox.Text.Trim();
        }

        if (_versionBox is not null && !string.IsNullOrWhiteSpace(_versionBox.Text))
            _document.Version = _versionBox.Text.Trim();
        if (_terrainDirBox is not null && !string.IsNullOrWhiteSpace(_terrainDirBox.Text))
            _document.TerrainDirectory = _terrainDirBox.Text.Trim();
        if (_gravestoneBox is not null && !string.IsNullOrWhiteSpace(_gravestoneBox.Text))
            _document.GravestoneRelativePath = _gravestoneBox.Text.Trim();

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
        _choiceOverlay.Close(notifyClosed: false);
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _settingsScroll = null;
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
        _settingsEntries.Clear();
        _menuEntries.Clear();
        _navigateRepeat.Reset();
        _focusZone = FocusZone.Settings;
        _settingsFocusIndex = 0;
        _menuFocusIndex = 0;
    }

    private void RebuildSettingsColumn()
    {
        if (_settingsHost is null)
            return;

        ApplyTextFields();
        _settingsHost.Visual.Children.Clear();
        _settingsEntries.Clear();

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
        AddSettingsButton("Add remap", CommitNewRemap);

        foreach (var remap in _document.Remaps.OrderBy(entry => entry.ContentIdFull, StringComparer.OrdinalIgnoreCase))
        {
            var captured = remap;
            var summary = captured.ContentIdFull + " → " + ShortPath(captured.BasePath);
            AddSettingsButton(summary, () => OpenRemapChoice(captured));
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

        var contentId = (_newContentIdBox.Text ?? string.Empty).Trim();
        var basePath = (_newBaseBox.Text ?? string.Empty).Trim();
        var maskPath = (_newMaskBox.Text ?? string.Empty).Trim();
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
            onClosed: () => SetFocusZone(FocusZone.Settings, resetIndex: false));
    }

    private static string ShortPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "(empty)";
        var normalized = path.Replace('\\', '/');
        return normalized.Length <= 42 ? normalized : "…" + normalized[^39..];
    }

    private void HandleSettingsInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_settingsEntries.Count == 0)
            return;

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

    private bool TrySwitchColumn(IGameCommandSource commands) =>
        EditorTwoColumnFormShell.TrySwitchTwoZones(
            commands,
            allowDpadColumnSwitch: true,
            currentZone: (int)_focusZone,
            settingsZone: (int)FocusZone.Settings,
            menuZone: (int)FocusZone.Menu,
            setZone: zone => SetFocusZone((FocusZone)zone, resetIndex: false));

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

        EditorFormScrollFocus.AfterNavigate(
            _settingsScroll,
            _settingsHost,
            _settingsEntries,
            listFocusStartIndex: 0,
            listFocusCount: _settingsEntries.Count,
            _settingsFocusIndex,
            StackSpacing,
            MinScrollViewport);
    }

    private void AddSettingsButton(string text, Action onClick)
    {
        if (_settingsHost is null)
            return;
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        _settingsHost.AddChild(button);
        _settingsEntries.Add((button, onClick));
        button.Click += (_, _) =>
        {
            for (var i = 0; i < _settingsEntries.Count; i++)
            {
                if (!ReferenceEquals(_settingsEntries[i].Button, button))
                    continue;
                _settingsFocusIndex = i;
                break;
            }

            SetFocusZone(FocusZone.Settings, resetIndex: false);
            onClick();
        };
    }
}
