using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Editor.Bundles;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Edit *.bundle.json: modules + defaults (left) + Save/Back (right).</summary>
public sealed class EditorBundleEditView
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
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private readonly List<(Button Button, Action Activate)> _settingsEntries = [];
    private readonly List<(Button Button, Action Activate)> _menuEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private readonly EditorChoiceOverlay _choiceOverlay = new();

    private FocusZone _focusZone = FocusZone.Settings;
    private int _settingsFocusIndex;
    private int _menuFocusIndex;
    private EditableBundleDocument _document = new();
    private IReadOnlyList<ContentModuleInfo> _availableModules = [];
    private bool _isNew;

    public bool IsTextEntryActive =>
        EditorGumTextEntry.IsAnyFocused(_idBox, _titleBox);

    public bool IsOverlayOpen => _choiceOverlay.IsOpen;

    public EditableBundleDocument Document => _document;

    public void Build(
        EditableBundleDocument document,
        IReadOnlyList<ContentModuleInfo> availableModules,
        bool isNew,
        Action onSave,
        Action onBack)
    {
        Clear();
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _availableModules = availableModules ?? [];
        _isNew = isNew;

        var built = EditorTwoColumnFormShell.Build(
            isNew ? "New Bundle" : "Edit Bundle — " + _document.Id,
            "Modules = preset contents. Defaults = New Game start composition (must be listed above). Theme is required.",
            maxShellWidthPixels: 780f);
        _rootPanel = built.RootPanel;
        _settingsHost = built.SettingsHost;
        _settingsScroll = built.SettingsScroll;

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        built.MenuHost.AddChild(_statusLabel);

        EditorTwoColumnFormShell.AddMenuButton(
            built.MenuHost,
            _menuEntries,
            "Save bundle",
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

    public void SyncIdentityFromDocument()
    {
        if (_idBox is not null)
            _idBox.Text = _document.Id;
        if (_titleBox is not null)
            _titleBox.Text = _document.Title;
    }

    public void ApplyTextFields()
    {
        if (_idBox is not null && !string.IsNullOrWhiteSpace(_idBox.Text))
            _document.Id = _idBox.Text.Trim();
        if (_titleBox is not null && !string.IsNullOrWhiteSpace(_titleBox.Text))
            _document.Title = _titleBox.Text.Trim();
        _document.IsDirty = true;
    }

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
            if (_menuEntries.Count == 0)
                return;
            GumFocusableButtonList.HandleVerticalInput(
                commands,
                _menuEntries,
                ref _menuFocusIndex,
                _navigateRepeat,
                elapsedSeconds);
            return;
        }

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

    public void Clear()
    {
        _choiceOverlay.Close(notifyClosed: false);
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _settingsScroll = null;
        _settingsHost = null;
        _statusLabel = null;
        _idBox = null;
        _titleBox = null;
        _settingsEntries.Clear();
        _menuEntries.Clear();
        _navigateRepeat.Reset();
        _focusZone = FocusZone.Settings;
        _settingsFocusIndex = 0;
        _menuFocusIndex = 0;
        _availableModules = [];
    }

    private void RebuildSettingsColumn()
    {
        if (_settingsHost is null)
            return;

        ApplyTextFields();
        SyncDefaultsWithListedModules();
        _settingsHost.Visual.Children.Clear();
        _settingsEntries.Clear();

        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Id (file stem)", _document.Id, out _idBox);
        if (!_isNew && _idBox is not null)
            _idBox.IsEnabled = false;

        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Title", _document.Title, out _titleBox);

        AddSectionLabel("Modules (toggle)");
        foreach (var module in _availableModules.OrderBy(module => module.ModuleId, StringComparer.OrdinalIgnoreCase))
        {
            var captured = module;
            var included = _document.ModuleIds.Contains(captured.ModuleId, StringComparer.Ordinal);
            var label = (included ? "[x] " : "[ ] ")
                + captured.ModuleId
                + " ("
                + captured.Type.ToString().ToLowerInvariant()
                + ")";
            AddSettingsButton(label, () => ToggleModule(captured));
        }

        if (_availableModules.Count == 0)
            AddSectionLabel("(No modules in library — install or create modules first.)");

        AddSectionLabel("Defaults (New Game start set from Modules above)");
        AddSettingsButton(
            "Scenario: " + DisplayOrNone(_document.ScenarioModuleId),
            () => OpenSingleDefaultChoice(
                "Choose scenario default",
                ContentModuleType.Scenario,
                () => _document.ScenarioModuleId,
                selected => _document.ScenarioModuleId = selected));
        AddSettingsButton(
            "Units: " + DisplayList(_document.UnitsModuleIds),
            () => OpenMultiDefaultChoice(
                "Toggle units default",
                ContentModuleType.Units,
                _document.UnitsModuleIds));
        AddSettingsButton(
            "Buildings: " + DisplayList(_document.BuildingsModuleIds),
            () => OpenMultiDefaultChoice(
                "Toggle buildings default",
                ContentModuleType.Buildings,
                _document.BuildingsModuleIds));
        AddSettingsButton(
            "Theme: " + DisplayOrNone(_document.ThemeModuleId),
            () => OpenSingleDefaultChoice(
                "Choose theme default",
                ContentModuleType.Theme,
                () => _document.ThemeModuleId,
                selected => _document.ThemeModuleId = selected));
    }

    private void ToggleModule(ContentModuleInfo module)
    {
        ApplyTextFields();
        var id = module.ModuleId;
        if (_document.ModuleIds.Contains(id, StringComparer.Ordinal))
        {
            _document.ModuleIds.RemoveAll(existing => string.Equals(existing, id, StringComparison.Ordinal));
        }
        else
        {
            _document.ModuleIds.Add(id);
        }

        SyncDefaultsWithListedModules();
        _document.IsDirty = true;
        RebuildSettingsColumn();
    }

    /// <summary>
    /// Drops defaults that point outside modules[]; fills empty defaults from listed modules of that type.
    /// </summary>
    private void SyncDefaultsWithListedModules()
    {
        var listed = _document.ModuleIds
            .Where(moduleId => !string.IsNullOrWhiteSpace(moduleId))
            .Select(moduleId => moduleId.Trim())
            .ToHashSet(StringComparer.Ordinal);

        if (!listed.Contains(_document.ScenarioModuleId))
            _document.ScenarioModuleId = string.Empty;
        if (!listed.Contains(_document.ThemeModuleId))
            _document.ThemeModuleId = string.Empty;

        _document.UnitsModuleIds.RemoveAll(moduleId => !listed.Contains(moduleId));
        _document.BuildingsModuleIds.RemoveAll(moduleId => !listed.Contains(moduleId));

        if (string.IsNullOrWhiteSpace(_document.ScenarioModuleId))
        {
            var scenario = FirstListedOfType(ContentModuleType.Scenario);
            if (scenario is not null)
                _document.ScenarioModuleId = scenario;
        }

        if (string.IsNullOrWhiteSpace(_document.ThemeModuleId))
        {
            var theme = FirstListedOfType(ContentModuleType.Theme);
            if (theme is not null)
                _document.ThemeModuleId = theme;
        }

        if (_document.UnitsModuleIds.Count == 0)
        {
            var units = FirstListedOfType(ContentModuleType.Units);
            if (units is not null)
                _document.UnitsModuleIds.Add(units);
        }

        if (_document.BuildingsModuleIds.Count == 0)
        {
            var buildings = FirstListedOfType(ContentModuleType.Buildings);
            if (buildings is not null)
                _document.BuildingsModuleIds.Add(buildings);
        }
    }

    private string? FirstListedOfType(ContentModuleType type) =>
        _document.ModuleIds
            .Select(moduleId => _availableModules.FirstOrDefault(
                module => string.Equals(module.ModuleId, moduleId, StringComparison.Ordinal)))
            .Where(module => module is not null && module.Type == type)
            .Select(module => module!.ModuleId)
            .FirstOrDefault();

    private void OpenSingleDefaultChoice(
        string title,
        ContentModuleType type,
        Func<string> getSelected,
        Action<string> apply)
    {
        if (_rootPanel is null)
            return;

        var candidates = ListedModulesOfType(type);
        if (candidates.Count == 0)
        {
            SyncStatus(
                "No "
                + type.ToString().ToLowerInvariant()
                + " module in Modules — toggle one above (e.g. vanilla_theme).");
            return;
        }

        var selected = getSelected() ?? string.Empty;
        var actions = candidates
            .Select(moduleId =>
            {
                var captured = moduleId;
                var included = string.Equals(captured, selected, StringComparison.Ordinal);
                var label = (included ? "[x] " : "[ ] ") + captured;
                return ((string Label, Action Activate))(label, () =>
                {
                    apply(captured);
                    _document.IsDirty = true;
                    RebuildSettingsColumn();
                });
            })
            .ToList();

        _choiceOverlay.Open(
            _rootPanel,
            title,
            actions,
            onClosed: () => SetFocusZone(FocusZone.Settings, resetIndex: false));
    }

    private void OpenMultiDefaultChoice(
        string title,
        ContentModuleType type,
        List<string> targetList)
    {
        if (_rootPanel is null)
            return;

        var candidates = ListedModulesOfType(type);
        if (candidates.Count == 0)
        {
            SyncStatus("Add a " + type.ToString().ToLowerInvariant() + " module to modules[] first.");
            return;
        }

        var actions = candidates
            .Select(moduleId =>
            {
                var captured = moduleId;
                var included = targetList.Contains(captured, StringComparer.Ordinal);
                var label = (included ? "[x] " : "[ ] ") + captured;
                return ((string Label, Action Activate))(label, () =>
                {
                    if (targetList.Contains(captured, StringComparer.Ordinal))
                        targetList.RemoveAll(existing => string.Equals(existing, captured, StringComparison.Ordinal));
                    else
                        targetList.Add(captured);
                    _document.IsDirty = true;
                    RebuildSettingsColumn();
                });
            })
            .ToList();

        _choiceOverlay.Open(
            _rootPanel,
            title,
            actions,
            onClosed: () => SetFocusZone(FocusZone.Settings, resetIndex: false));
    }

    private IReadOnlyList<string> ListedModulesOfType(ContentModuleType type) =>
        _document.ModuleIds
            .Where(moduleId =>
            {
                var info = _availableModules.FirstOrDefault(
                    module => string.Equals(module.ModuleId, moduleId, StringComparison.Ordinal));
                return info is not null && info.Type == type;
            })
            .OrderBy(moduleId => moduleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string DisplayOrNone(string value) =>
        string.IsNullOrWhiteSpace(value) ? "(none)" : value;

    private static string DisplayList(IReadOnlyList<string> values) =>
        values.Count == 0 ? "(none)" : string.Join(", ", values);

    private void AddSectionLabel(string text)
    {
        if (_settingsHost is null)
            return;
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        _settingsHost.AddChild(label);
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
        if (_settingsEntries.Count == 0 || _settingsScroll is null || _settingsHost is null)
            return;

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
