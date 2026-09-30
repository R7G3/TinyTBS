using Gum;
using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Edit Buildings/{id}.json: settings scroll (left) + Save/Back menu (right).</summary>
public sealed class EditorBuildingEditView
{
    private readonly EditorTwoColumnFormController _form = new(allowDpadColumnSwitch: false);

    private Panel? _rootPanel;
    private Panel? _settingsHost;
    private Label? _statusLabel;
    private TextBox? _idBox;
    private TextBox? _displayNameBox;
    private TextBox? _spriteBaseBox;
    private TextBox? _spriteMaskBox;
    private TextBox? _spriteRuinedBaseBox;
    private TextBox? _spriteRuinedMaskBox;
    private TextBox? _newTagBox;
    private TextBox? _newRecruitTagBox;
    private readonly EditorChoiceOverlay _choiceOverlay = new();

    private EditableBuildingDocument _document = EditableBuildingDocument.CreateDefault("building");

    private Label? _tagsSummaryLabel;
    private Label? _recruitTagsSummaryLabel;
    private Label? _incomeLabel;
    private Label? _defenceBonusLabel;
    private Label? _healAmountLabel;
    private Label? _healScopeLabel;
    private Label? _ruinedIncomeLabel;
    private Label? _ruinedDefenceLabel;
    private Label? _ruinedHealAmountLabel;
    private Label? _ruinedHealScopeLabel;

    public bool IsTextEntryActive =>
        EditorGumTextEntry.IsAnyFocused(
            _idBox,
            _displayNameBox,
            _spriteBaseBox,
            _spriteMaskBox,
            _spriteRuinedBaseBox,
            _spriteRuinedMaskBox,
            _newTagBox,
            _newRecruitTagBox);

    public bool IsOverlayOpen => _choiceOverlay.IsOpen;

    public EditableBuildingDocument Document => _document;

    public void Build(EditableBuildingDocument document, Action onSave, Action onBack)
    {
        Clear();
        _document = document ?? throw new ArgumentNullException(nameof(document));

        var built = EditorTwoColumnFormShell.Build(
            "Edit Building",
            "LB/RB: columns. L/R: −/+ on steppers. Up/Down: rows.");
        _rootPanel = built.RootPanel;
        _settingsHost = built.SettingsHost;
        _form.Attach(built);

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        built.MenuHost.AddChild(_statusLabel);

        _form.AddMenuButton(
            built.MenuHost,
            "Save building",
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

    public void SyncIdentityFromDocument()
    {
        if (_idBox is not null)
            _idBox.Text = _document.Id;
        if (_displayNameBox is not null)
            _displayNameBox.Text = _document.DisplayNameKey;
    }

    public void ApplyTextFields()
    {
        if (_idBox is not null && !string.IsNullOrWhiteSpace(_idBox.Text))
            _document.Id = _idBox.Text;
        if (_displayNameBox is not null)
            _document.DisplayNameKey = string.IsNullOrWhiteSpace(_displayNameBox.Text)
                ? "buildings." + _document.Id
                : _displayNameBox.Text;
        if (_spriteBaseBox is not null && !string.IsNullOrWhiteSpace(_spriteBaseBox.Text))
            _document.SpriteBase = _spriteBaseBox.Text;
        if (_spriteMaskBox is not null && !string.IsNullOrWhiteSpace(_spriteMaskBox.Text))
            _document.SpriteMask = _spriteMaskBox.Text;
        if (_spriteRuinedBaseBox is not null)
            _document.SpriteRuinedBase = string.IsNullOrWhiteSpace(_spriteRuinedBaseBox.Text)
                ? null
                : _spriteRuinedBaseBox.Text;
        if (_spriteRuinedMaskBox is not null)
            _document.SpriteRuinedMask = string.IsNullOrWhiteSpace(_spriteRuinedMaskBox.Text)
                ? null
                : _spriteRuinedMaskBox.Text;
        if (_newTagBox is not null && !string.IsNullOrWhiteSpace(_newTagBox.Text))
            TryAddTag(_document.Tags, _newTagBox.Text);
        if (_newRecruitTagBox is not null && !string.IsNullOrWhiteSpace(_newRecruitTagBox.Text))
            TryAddTag(_document.RecruitFromTags, _newRecruitTagBox.Text);
        _document.IsDirty = true;
    }

    public bool TryCloseOverlay() => _choiceOverlay.TryClose();

    public void HandleInput(IGameCommandSource commands, IPointerSource pointer, float elapsedSeconds)
    {
        if (_choiceOverlay.IsOpen)
        {
            _choiceOverlay.HandleInput(commands, elapsedSeconds);
            return;
        }

        if (IsTextEntryActive)
            return;

        _form.HandleInput(commands, elapsedSeconds, pointer);
    }

    public void Clear()
    {
        _choiceOverlay.Close(notifyClosed: false);
        GumService.Default.Root.Children.Clear();
        _form.Clear();
        _rootPanel = null;
        _settingsHost = null;
        _statusLabel = null;
        _idBox = null;
        _displayNameBox = null;
        _spriteBaseBox = null;
        _spriteMaskBox = null;
        _spriteRuinedBaseBox = null;
        _spriteRuinedMaskBox = null;
        _newTagBox = null;
        _newRecruitTagBox = null;
    }

    private void RebuildFromDocument()
    {
        ApplyTextFields();
        RebuildSettingsColumn();
    }

    private void RebuildSettingsColumn()
    {
        if (_settingsHost is not null)
            _form.RebuildSettings(AddSettingsRows);
    }

    private void AddSettingsRows()
    {
        if (_settingsHost is null)
            return;

        AddSectionLabel("Identity");
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Building Id (Buildings/*.json)", _document.Id, out _idBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Display name key", _document.DisplayNameKey, out _displayNameBox);

        AddSectionLabel("Tags");
        _tagsSummaryLabel = new Label { Text = TagsCaption(_document.Tags, "Tags") };
        GumUiLayout.FillParentWidth(_tagsSummaryLabel);
        _settingsHost.AddChild(_tagsSummaryLabel);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "New tag", string.Empty, out _newTagBox);
        AddSettingsButton("Add tag", () => CommitTagList(isRecruit: false));
        foreach (var tag in _document.Tags.ToList())
        {
            var captured = tag;
            AddSettingsButton("Tag: " + captured, () => OpenTagChoice(_document.Tags, captured, isRecruit: false));
        }

        AddSectionLabel("Recruit from tags");
        _recruitTagsSummaryLabel = new Label { Text = TagsCaption(_document.RecruitFromTags, "Recruit tags") };
        GumUiLayout.FillParentWidth(_recruitTagsSummaryLabel);
        _settingsHost.AddChild(_recruitTagsSummaryLabel);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "New recruit tag", string.Empty, out _newRecruitTagBox);
        AddSettingsButton("Add recruit tag", () => CommitTagList(isRecruit: true));
        foreach (var tag in _document.RecruitFromTags.ToList())
        {
            var captured = tag;
            AddSettingsButton("Recruit tag: " + captured, () => OpenTagChoice(_document.RecruitFromTags, captured, isRecruit: true));
        }

        AddSectionLabel("Sprites");
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Sprite base", _document.SpriteBase, out _spriteBaseBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Sprite mask", _document.SpriteMask, out _spriteMaskBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Ruined base", _document.SpriteRuinedBase ?? string.Empty, out _spriteRuinedBaseBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Ruined mask", _document.SpriteRuinedMask ?? string.Empty, out _spriteRuinedMaskBox);

        AddSectionLabel("Economy & defence");
        _incomeLabel = AddStatLabel("Income: " + _document.Income);
        AddStatStepper(() => AdjustIncome(-5), () => AdjustIncome(+5));
        _defenceBonusLabel = AddStatLabel("Defence bonus: " + _document.DefenceBonus);
        AddStatStepper(() => AdjustDefenceBonus(-1), () => AdjustDefenceBonus(+1));

        AddSectionLabel("Flags");
        AddSettingsButton(AllowsRecruitCaption(), ToggleAllowsRecruit);
        AddSettingsButton(CapturableCaption(), ToggleCapturable);
        AddSettingsButton(DestroyableCaption(), ToggleDestroyable);
        AddSettingsButton(RepairableCaption(), ToggleRepairable);
        AddSettingsButton(CountsDefeatCaption(), ToggleCountsDefeat);

        AddSectionLabel("Heal");
        AddSettingsButton(HealEnabledCaption(), ToggleHealEnabled);
        if (_document.Heal is not null)
        {
            _healAmountLabel = AddStatLabel("Heal amount: " + _document.Heal.Amount);
            AddStatStepper(() => AdjustHealAmount(-5), () => AdjustHealAmount(+5));
            _healScopeLabel = new Label { Text = HealScopeCaption(_document.Heal.Scope) };
            GumUiLayout.FillParentWidth(_healScopeLabel);
            _settingsHost.AddChild(_healScopeLabel);
            AddSettingsButton("Choose heal scope…", () => OpenHealScopeChoice(isRuined: false));
        }

        AddSectionLabel("Ruined state");
        AddSettingsButton(HasRuinedCaption(), ToggleHasRuined);
        if (_document.HasRuined)
        {
            _document.Ruined ??= new EditableBuildingRuined();
            _ruinedIncomeLabel = AddStatLabel("Ruined income: " + _document.Ruined.Income);
            AddStatStepper(() => AdjustRuinedIncome(-5), () => AdjustRuinedIncome(+5));
            _ruinedDefenceLabel = AddStatLabel("Ruined defence bonus: " + _document.Ruined.DefenceBonus);
            AddStatStepper(() => AdjustRuinedDefence(-1), () => AdjustRuinedDefence(+1));
            AddSettingsButton(RuinedCapturableCaption(), ToggleRuinedCapturable);

            if (_document.Ruined.Heal is null)
                _document.Ruined.Heal = new EditableBuildingHeal { Amount = 0, Scope = BuildingHealScopeIds.None };

            _ruinedHealAmountLabel = AddStatLabel("Ruined heal amount: " + _document.Ruined.Heal.Amount);
            AddStatStepper(() => AdjustRuinedHealAmount(-5), () => AdjustRuinedHealAmount(+5));
            _ruinedHealScopeLabel = new Label { Text = HealScopeCaption(_document.Ruined.Heal.Scope, prefix: "Ruined heal") };
            GumUiLayout.FillParentWidth(_ruinedHealScopeLabel);
            _settingsHost.AddChild(_ruinedHealScopeLabel);
            AddSettingsButton("Choose ruined heal scope…", () => OpenHealScopeChoice(isRuined: true));
        }
    }

    private Label AddStatLabel(string text)
    {
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        _settingsHost!.AddChild(label);
        return label;
    }

    private void AddStatStepper(Action onDecrease, Action onIncrease) => _form.AddStepper(onDecrease, onIncrease);

    private void AddSettingsButton(string text, Action onClick) => _form.AddSettingsButton(text, onClick);

    private void AddSectionLabel(string text)
    {
        var label = new Label { Text = "— " + text + " —" };
        GumUiLayout.FillParentWidth(label);
        _settingsHost!.AddChild(label);
    }

    private void CommitTagList(bool isRecruit)
    {
        // Capture then rebuild (ApplyTextFields inside RebuildFromDocument adds the tag).
        RebuildFromDocument();
    }

    private static void TryAddTag(List<string> tags, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;
        if (tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            return;
        tags.Add(tag);
    }

    private void OpenTagChoice(List<string> tags, string tag, bool isRecruit)
    {
        if (_rootPanel is null)
            return;

        _choiceOverlay.Open(
            _rootPanel,
            tag,
            [
                ("Remove", () =>
                {
                    tags.RemoveAll(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase));
                    _document.IsDirty = true;
                    RebuildFromDocument();
                }),
            ],
            onClosed: () => _form.FocusSettings());
    }

    private void OpenHealScopeChoice(bool isRuined)
    {
        if (_rootPanel is null)
            return;

        var actions = new List<(string Label, Action Activate)>();
        foreach (var scope in BuildingHealScopeIds.All)
        {
            var captured = scope;
            actions.Add((scope, () =>
            {
                if (isRuined)
                {
                    if (_document.Ruined?.Heal is null)
                        return;
                    _document.Ruined.Heal.Scope = captured;
                    if (_ruinedHealScopeLabel is not null)
                        _ruinedHealScopeLabel.Text = HealScopeCaption(captured, prefix: "Ruined heal");
                }
                else
                {
                    if (_document.Heal is null)
                        return;
                    _document.Heal.Scope = captured;
                    if (_healScopeLabel is not null)
                        _healScopeLabel.Text = HealScopeCaption(captured);
                }

                _document.IsDirty = true;
                RebuildFromDocument();
            }));
        }

        _choiceOverlay.Open(
            _rootPanel,
            isRuined ? "Ruined heal scope" : "Heal scope",
            actions,
            onClosed: () => _form.FocusSettings());
    }

    private void ToggleAllowsRecruit()
    {
        _document.AllowsRecruit = !_document.AllowsRecruit;
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void ToggleCapturable()
    {
        _document.Capturable = !_document.Capturable;
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void ToggleDestroyable()
    {
        _document.Destroyable = !_document.Destroyable;
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void ToggleRepairable()
    {
        _document.Repairable = !_document.Repairable;
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void ToggleCountsDefeat()
    {
        _document.CountsTowardPlayerDefeat = !_document.CountsTowardPlayerDefeat;
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void ToggleHealEnabled()
    {
        if (_document.Heal is null)
            _document.Heal = new EditableBuildingHeal { Amount = 20, Scope = BuildingHealScopeIds.Allied };
        else
            _document.Heal = null;
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void ToggleHasRuined()
    {
        _document.HasRuined = !_document.HasRuined;
        if (_document.HasRuined)
            _document.Ruined ??= new EditableBuildingRuined();
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void ToggleRuinedCapturable()
    {
        if (_document.Ruined is null)
            return;
        _document.Ruined.Capturable = !_document.Ruined.Capturable;
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void AdjustIncome(int delta)
    {
        _document.Income = Math.Max(0, _document.Income + delta);
        _document.IsDirty = true;
        if (_incomeLabel is not null)
            _incomeLabel.Text = "Income: " + _document.Income;
    }

    private void AdjustDefenceBonus(int delta)
    {
        _document.DefenceBonus = Math.Max(0, _document.DefenceBonus + delta);
        _document.IsDirty = true;
        if (_defenceBonusLabel is not null)
            _defenceBonusLabel.Text = "Defence bonus: " + _document.DefenceBonus;
    }

    private void AdjustHealAmount(int delta)
    {
        if (_document.Heal is null)
            return;
        _document.Heal.Amount = Math.Max(0, _document.Heal.Amount + delta);
        _document.IsDirty = true;
        if (_healAmountLabel is not null)
            _healAmountLabel.Text = "Heal amount: " + _document.Heal.Amount;
    }

    private void AdjustRuinedIncome(int delta)
    {
        if (_document.Ruined is null)
            return;
        _document.Ruined.Income = Math.Max(0, _document.Ruined.Income + delta);
        _document.IsDirty = true;
        if (_ruinedIncomeLabel is not null)
            _ruinedIncomeLabel.Text = "Ruined income: " + _document.Ruined.Income;
    }

    private void AdjustRuinedDefence(int delta)
    {
        if (_document.Ruined is null)
            return;
        _document.Ruined.DefenceBonus = Math.Max(0, _document.Ruined.DefenceBonus + delta);
        _document.IsDirty = true;
        if (_ruinedDefenceLabel is not null)
            _ruinedDefenceLabel.Text = "Ruined defence bonus: " + _document.Ruined.DefenceBonus;
    }

    private void AdjustRuinedHealAmount(int delta)
    {
        if (_document.Ruined?.Heal is null)
            return;
        _document.Ruined.Heal.Amount = Math.Max(0, _document.Ruined.Heal.Amount + delta);
        _document.IsDirty = true;
        if (_ruinedHealAmountLabel is not null)
            _ruinedHealAmountLabel.Text = "Ruined heal amount: " + _document.Ruined.Heal.Amount;
    }

    private static string TagsCaption(IReadOnlyList<string> tags, string prefix) =>
        tags.Count == 0 ? prefix + ": (none)" : prefix + ": " + string.Join(", ", tags);

    private static string HealScopeCaption(string scope, string prefix = "Heal") =>
        prefix + " scope: " + scope;

    private string AllowsRecruitCaption() => "Allows recruit: " + (_document.AllowsRecruit ? "On" : "Off");

    private string CapturableCaption() => "Capturable: " + (_document.Capturable ? "On" : "Off");

    private string DestroyableCaption() => "Destroyable: " + (_document.Destroyable ? "On" : "Off");

    private string RepairableCaption() => "Repairable: " + (_document.Repairable ? "On" : "Off");

    private string CountsDefeatCaption() =>
        "Counts toward player defeat: " + (_document.CountsTowardPlayerDefeat ? "On" : "Off");

    private string HealEnabledCaption() => "Heal enabled: " + (_document.Heal is not null ? "On" : "Off");

    private string HasRuinedCaption() => "Has ruined state: " + (_document.HasRuined ? "On" : "Off");

    private string RuinedCapturableCaption() =>
        "Ruined capturable: " + (_document.Ruined?.Capturable == true ? "On" : "Off");
}
