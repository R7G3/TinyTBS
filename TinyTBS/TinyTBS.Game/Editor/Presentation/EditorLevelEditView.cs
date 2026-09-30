using Gum;
using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Editor.Levels;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>
/// Edit level.json: settings list (left) + action menu (right), campaign-style columns.
/// </summary>
public sealed class EditorLevelEditView
{
    private readonly EditorTwoColumnFormController _form = new(allowDpadColumnSwitch: false);

    private Panel? _settingsHost;
    private Label? _statusLabel;
    private Label? _mapLabel;
    private Label? _playersLabel;
    private Label? _goldLabel;
    private Label? _capLabel;
    private Label? _modesLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private EditableLevelDocument _document = EditableLevelDocument.CreateDefault("level", "level", "map");
    private IReadOnlyList<string> _mapIds = [];
    private int _mapIndex;

    public bool IsTextEntryActive =>
        _idBox is { IsFocused: true } || _titleBox is { IsFocused: true };

    public EditableLevelDocument Document => _document;

    public void Build(
        EditableLevelDocument document,
        IReadOnlyList<string> mapIds,
        Action onSave,
        Action onBack)
    {
        Clear();
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _mapIds = mapIds ?? [];
        _mapIndex = IndexOfMap(document.MapIdFromRef());

        var built = EditorTwoColumnFormShell.Build(
            "Edit Level",
            "LB/RB: columns. Left/Right: −/+. Up/Down: next stepper row.");
        _settingsHost = built.SettingsHost;
        _form.Attach(built);

        _modesLabel = new Label { Text = ModesCaption() };
        GumUiLayout.FillParentWidth(_modesLabel);
        built.MenuHost.AddChild(_modesLabel);
        _form.AddMenuButton(built.MenuHost, "Toggle Skirmish", () => ToggleMode(LevelModeIds.Skirmish));
        _form.AddMenuButton(built.MenuHost, "Toggle Campaign", () => ToggleMode(LevelModeIds.Campaign));

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        built.MenuHost.AddChild(_statusLabel);

        _form.AddMenuButton(
            built.MenuHost,
            "Save",
            () =>
            {
                ApplyTextFields();
                onSave();
            });
        _form.AddMenuButton(built.MenuHost, "Back", onBack);

        _form.RebuildSettings(AddSettingsRows);
        _form.FocusSettings(resetIndex: true);
    }

    public void SyncStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text ?? string.Empty;
    }

    public void ApplyTextFields()
    {
        if (_idBox is not null && !string.IsNullOrWhiteSpace(_idBox.Text))
            _document.Id = _idBox.Text;
        if (_titleBox is not null)
            _document.Title = string.IsNullOrWhiteSpace(_titleBox.Text) ? _document.Id : _titleBox.Text;
        _document.IsDirty = true;
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
        _settingsHost = null;
        _statusLabel = null;
        _mapLabel = null;
        _playersLabel = null;
        _goldLabel = null;
        _capLabel = null;
        _modesLabel = null;
        _idBox = null;
        _titleBox = null;
        _mapIds = [];
        _mapIndex = 0;
    }

    private void AddSettingsRows()
    {
        if (_settingsHost is null)
            return;

        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Level Id (Levels/ folder name)", _document.Id, out _idBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Title (display name)", _document.Title, out _titleBox);

        _mapLabel = AddLabel(MapCaption());
        _form.AddSettingsButton("Prev Map", () => CycleMap(-1));
        _form.AddSettingsButton("Next Map", () => CycleMap(+1));

        _playersLabel = AddLabel(PlayersCaption());
        AddLabel("Min players");
        _form.AddStepper(() => AdjustPlayersMin(-1), () => AdjustPlayersMin(+1));
        AddLabel("Max players");
        _form.AddStepper(() => AdjustPlayersMax(-1), () => AdjustPlayersMax(+1));
        AddLabel("Default slots");
        _form.AddStepper(() => AdjustDefaultSlots(-1), () => AdjustDefaultSlots(+1));

        _goldLabel = AddLabel(GoldCaption());
        _form.AddStepper(() => AdjustGold(-50), () => AdjustGold(+50));

        _capLabel = AddLabel(CapCaption());
        _form.AddStepper(() => AdjustCap(-1), () => AdjustCap(+1));
    }

    private Label AddLabel(string text)
    {
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        _settingsHost!.AddChild(label);
        return label;
    }

    private void CycleMap(int delta)
    {
        if (_mapIds.Count == 0)
            return;
        _mapIndex = (_mapIndex + delta + _mapIds.Count) % _mapIds.Count;
        _document.SetMapId(_mapIds[_mapIndex]);
        if (_mapLabel is not null)
            _mapLabel.Text = MapCaption();
    }

    private void AdjustPlayersMin(int delta)
    {
        var min = Math.Clamp(_document.PlayersMin + delta, 1, 8);
        _document.PlayersMin = min;
        if (_document.PlayersMax < min)
            _document.PlayersMax = min;
        if (_document.DefaultSlots < min)
            _document.DefaultSlots = min;
        if (_document.DefaultSlots > _document.PlayersMax)
            _document.DefaultSlots = _document.PlayersMax;
        _document.IsDirty = true;
        RefreshPlayerLabel();
    }

    private void AdjustPlayersMax(int delta)
    {
        var max = Math.Clamp(_document.PlayersMax + delta, 1, 8);
        _document.PlayersMax = max;
        if (_document.PlayersMin > max)
            _document.PlayersMin = max;
        if (_document.DefaultSlots > max)
            _document.DefaultSlots = max;
        if (_document.DefaultSlots < _document.PlayersMin)
            _document.DefaultSlots = _document.PlayersMin;
        _document.IsDirty = true;
        RefreshPlayerLabel();
    }

    private void AdjustDefaultSlots(int delta)
    {
        var slots = Math.Clamp(_document.DefaultSlots + delta, _document.PlayersMin, _document.PlayersMax);
        _document.DefaultSlots = slots;
        _document.IsDirty = true;
        RefreshPlayerLabel();
    }

    private void AdjustGold(int delta)
    {
        _document.DefaultStartingGold = Math.Max(0, _document.DefaultStartingGold + delta);
        _document.IsDirty = true;
        if (_goldLabel is not null)
            _goldLabel.Text = GoldCaption();
    }

    private void AdjustCap(int delta)
    {
        _document.DefaultUnitCap = Math.Max(1, _document.DefaultUnitCap + delta);
        _document.IsDirty = true;
        if (_capLabel is not null)
            _capLabel.Text = CapCaption();
    }

    private void RefreshPlayerLabel()
    {
        if (_playersLabel is not null)
            _playersLabel.Text = PlayersCaption();
    }

    private void ToggleMode(string mode)
    {
        _document.SetMode(mode, !_document.HasMode(mode));
        if (_modesLabel is not null)
            _modesLabel.Text = ModesCaption();
    }

    private int IndexOfMap(string mapId)
    {
        for (var i = 0; i < _mapIds.Count; i++)
        {
            if (string.Equals(_mapIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return 0;
    }

    private string MapCaption() =>
        _mapIds.Count == 0
            ? "Map: (no maps — create a map first)"
            : $"Map: {_document.MapRef} ({_mapIndex + 1}/{_mapIds.Count})";

    private string PlayersCaption() =>
        $"Players: min {_document.PlayersMin}, max {_document.PlayersMax}, default slots {_document.DefaultSlots}";

    private string GoldCaption() => $"Starting gold: {_document.DefaultStartingGold}";

    private string CapCaption() => $"Unit cap: {_document.DefaultUnitCap}";

    private string ModesCaption() => "Modes: " + string.Join(", ", _document.Modes);
}
