using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Edit Buildings/{id}.json: settings scroll (left) + Save/Back menu (right).</summary>
public sealed class EditorBuildingEditView
{
    private enum FocusZone
    {
        Settings = 0,
        Menu = 1,
    }

    private static readonly string[] HealScopes = ["none", "allied", "any"];

    private const float StackSpacing = 6f;
    private const float MinScrollViewport = 120f;

    private Panel? _rootPanel;
    private ScrollViewer? _settingsScroll;
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
    private readonly List<(Button Button, Action Activate)> _settingsEntries = [];
    private readonly List<(Button Button, Action Activate)> _menuEntries = [];
    private readonly List<(Button Decrease, Button Increase)> _stepperRows = [];
    private readonly HashSet<Button> _stepperButtons = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private readonly EditorChoiceOverlay _choiceOverlay = new();

    private FocusZone _focusZone = FocusZone.Settings;
    private int _settingsFocusIndex;
    private int _menuFocusIndex;
    private EditableBuildingDocument _document = EditableBuildingDocument.CreateDefault("building");

    private float _confirmRepeatTimer;
    private float _pointerRepeatTimer;
    private int _pointerHoldFocusIndex = -1;

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
        _settingsScroll = built.SettingsScroll;

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        built.MenuHost.AddChild(_statusLabel);

        EditorTwoColumnFormShell.AddMenuButton(
            built.MenuHost,
            _menuEntries,
            "Save building",
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
        if (_displayNameBox is not null)
            _displayNameBox.Text = _document.DisplayNameKey;
    }

    public void ApplyTextFields()
    {
        if (_idBox is not null && !string.IsNullOrWhiteSpace(_idBox.Text))
            _document.Id = _idBox.Text.Trim();
        if (_displayNameBox is not null)
            _document.DisplayNameKey = string.IsNullOrWhiteSpace(_displayNameBox.Text)
                ? "buildings." + _document.Id
                : _displayNameBox.Text.Trim();
        if (_spriteBaseBox is not null && !string.IsNullOrWhiteSpace(_spriteBaseBox.Text))
            _document.SpriteBase = _spriteBaseBox.Text.Trim();
        if (_spriteMaskBox is not null && !string.IsNullOrWhiteSpace(_spriteMaskBox.Text))
            _document.SpriteMask = _spriteMaskBox.Text.Trim();
        if (_spriteRuinedBaseBox is not null)
            _document.SpriteRuinedBase = string.IsNullOrWhiteSpace(_spriteRuinedBaseBox.Text)
                ? null
                : _spriteRuinedBaseBox.Text.Trim();
        if (_spriteRuinedMaskBox is not null)
            _document.SpriteRuinedMask = string.IsNullOrWhiteSpace(_spriteRuinedMaskBox.Text)
                ? null
                : _spriteRuinedMaskBox.Text.Trim();
        if (_newTagBox is not null && !string.IsNullOrWhiteSpace(_newTagBox.Text))
            TryAddTag(_document.Tags, _newTagBox.Text.Trim());
        if (_newRecruitTagBox is not null && !string.IsNullOrWhiteSpace(_newRecruitTagBox.Text))
            TryAddTag(_document.RecruitFromTags, _newRecruitTagBox.Text.Trim());
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

        TickPointerHold(pointer, elapsedSeconds);
        TryBeginPointerHold(pointer);

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
        _idBox = null;
        _displayNameBox = null;
        _spriteBaseBox = null;
        _spriteMaskBox = null;
        _spriteRuinedBaseBox = null;
        _spriteRuinedMaskBox = null;
        _newTagBox = null;
        _newRecruitTagBox = null;
        _settingsEntries.Clear();
        _menuEntries.Clear();
        _stepperRows.Clear();
        _stepperButtons.Clear();
        _navigateRepeat.Reset();
        _focusZone = FocusZone.Settings;
        _settingsFocusIndex = 0;
        _menuFocusIndex = 0;
        _confirmRepeatTimer = 0f;
        ClearPointerHold();
    }

    private void RebuildFromDocument()
    {
        ApplyTextFields();
        RebuildSettingsColumn();
    }

    private void RebuildSettingsColumn()
    {
        if (_settingsHost is null)
            return;

        var previousFocus = _settingsFocusIndex;
        _settingsEntries.Clear();
        _stepperRows.Clear();
        _stepperButtons.Clear();
        _settingsHost.Visual.Children.Clear();

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
                _document.Ruined.Heal = new EditableBuildingHeal { Amount = 0, Scope = "none" };

            _ruinedHealAmountLabel = AddStatLabel("Ruined heal amount: " + _document.Ruined.Heal.Amount);
            AddStatStepper(() => AdjustRuinedHealAmount(-5), () => AdjustRuinedHealAmount(+5));
            _ruinedHealScopeLabel = new Label { Text = HealScopeCaption(_document.Ruined.Heal.Scope, prefix: "Ruined heal") };
            GumUiLayout.FillParentWidth(_ruinedHealScopeLabel);
            _settingsHost.AddChild(_ruinedHealScopeLabel);
            AddSettingsButton("Choose ruined heal scope…", () => OpenHealScopeChoice(isRuined: true));
        }

        if (_settingsEntries.Count > 0)
        {
            _settingsFocusIndex = Math.Clamp(previousFocus, 0, _settingsEntries.Count - 1);
            GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
            EnsureSettingsRowVisible();
        }
    }

    private Label AddStatLabel(string text)
    {
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        _settingsHost!.AddChild(label);
        return label;
    }

    private void AddStatStepper(Action onDecrease, Action onIncrease)
    {
        AddStepperRow(_settingsHost!, onDecrease, onIncrease, out var decrease, out var increase);
        _stepperRows.Add((decrease, increase));
    }

    private void AddSectionLabel(string text)
    {
        var label = new Label { Text = "— " + text + " —" };
        GumUiLayout.FillParentWidth(label);
        _settingsHost!.AddChild(label);
    }

    private void HandleSettingsInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_settingsEntries.Count == 0)
            return;

        if (IsStepperFocused()
            && HeldCommandRepeat.TryTick(commands, GameCommand.Confirm, elapsedSeconds, ref _confirmRepeatTimer))
        {
            _settingsEntries[_settingsFocusIndex].Activate();
            return;
        }

        if (!IsStepperFocused())
            _confirmRepeatTimer = 0f;

        if (IsStepperFocused())
        {
            var horizontal = _navigateRepeat.TryGetHorizontalDelta(commands, elapsedSeconds);
            if (horizontal != 0 && TryMoveWithinStepperRow(horizontal))
            {
                EnsureSettingsRowVisible();
                return;
            }

            var vertical = _navigateRepeat.TryGetDelta(commands, elapsedSeconds);
            if (vertical != 0)
            {
                TryMoveStepperRowVertically(vertical);
                EnsureSettingsRowVisible();
                return;
            }

            if (commands.WasPressed(GameCommand.Confirm))
            {
                _settingsEntries[_settingsFocusIndex].Activate();
                return;
            }

            GumFocusableButtonList.MaintainFocus(_settingsEntries, ref _settingsFocusIndex);
            return;
        }

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
            allowDpadColumnSwitch: false,
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

    private void CommitTagList(bool isRecruit)
    {
        // Capture then rebuild (ApplyTextFields inside RebuildFromDocument adds the tag).
        RebuildFromDocument();
    }

    private static void TryAddTag(List<string> tags, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;
        var trimmed = tag.Trim();
        if (tags.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            return;
        tags.Add(trimmed);
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
            onClosed: () => SetFocusZone(FocusZone.Settings, resetIndex: false));
    }

    private void OpenHealScopeChoice(bool isRuined)
    {
        if (_rootPanel is null)
            return;

        var actions = new List<(string Label, Action Activate)>();
        foreach (var scope in HealScopes)
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
            onClosed: () => SetFocusZone(FocusZone.Settings, resetIndex: false));
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
            _document.Heal = new EditableBuildingHeal { Amount = 20, Scope = "allied" };
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


    private void AddStepperRow(
        Panel parent,
        Action onDecrease,
        Action onIncrease,
        out Button decreaseButton,
        out Button increaseButton)
    {
        var row = new Panel();
        GumUiLayout.FillParentWidth(row);
        row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.Visual.StackSpacing = 8f;
        parent.AddChild(row);

        decreaseButton = CreateStepperButton(row, "-", onDecrease);
        increaseButton = CreateStepperButton(row, "+", onIncrease);
    }

    private Button CreateStepperButton(Panel row, string text, Action onActivate)
    {
        var button = new Button { Text = text };
        GumUiLayout.SetAbsoluteWidth(button, 72f);
        row.AddChild(button);
        _settingsEntries.Add((button, onActivate));
        _stepperButtons.Add(button);
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
        };
        return button;
    }

    private bool IsStepperFocused() =>
        _focusZone == FocusZone.Settings
        && _settingsFocusIndex >= 0
        && _settingsFocusIndex < _settingsEntries.Count
        && _stepperButtons.Contains(_settingsEntries[_settingsFocusIndex].Button);

    private bool TryMoveWithinStepperRow(int horizontalDelta)
    {
        if (!TryGetStepperPlacement(out var rowIndex, out var onIncrease))
            return false;

        var targetIncrease = horizontalDelta > 0;
        if (targetIncrease == onIncrease)
            return true;

        return FocusStepper(rowIndex, targetIncrease);
    }

    private void TryMoveStepperRowVertically(int verticalDelta)
    {
        if (!TryGetStepperPlacement(out var rowIndex, out var onIncrease))
            return;

        var currentIndex = _settingsFocusIndex;
        if (verticalDelta < 0)
        {
            var previousIndex = currentIndex - 1;
            if (previousIndex < 0)
                return;

            if (rowIndex > 0)
            {
                var previousRowIncrease = _stepperRows[rowIndex - 1].Increase;
                if (ReferenceEquals(_settingsEntries[previousIndex].Button, previousRowIncrease))
                {
                    FocusStepper(rowIndex - 1, onIncrease);
                    return;
                }
            }

            _settingsFocusIndex = previousIndex;
            GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
            return;
        }

        var nextIndex = currentIndex + 1;
        if (nextIndex >= _settingsEntries.Count)
            return;

        if (rowIndex + 1 < _stepperRows.Count)
        {
            var nextRowDecrease = _stepperRows[rowIndex + 1].Decrease;
            if (ReferenceEquals(_settingsEntries[nextIndex].Button, nextRowDecrease))
            {
                FocusStepper(rowIndex + 1, onIncrease);
                return;
            }
        }

        _settingsFocusIndex = nextIndex;
        GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
    }

    private int IndexOfSettingsButton(Button button)
    {
        for (var i = 0; i < _settingsEntries.Count; i++)
        {
            if (ReferenceEquals(_settingsEntries[i].Button, button))
                return i;
        }

        return -1;
    }

    private bool TryGetStepperPlacement(out int rowIndex, out bool onIncrease)
    {
        rowIndex = -1;
        onIncrease = false;
        if (_settingsFocusIndex < 0 || _settingsFocusIndex >= _settingsEntries.Count)
            return false;

        var button = _settingsEntries[_settingsFocusIndex].Button;
        for (var i = 0; i < _stepperRows.Count; i++)
        {
            if (ReferenceEquals(button, _stepperRows[i].Decrease))
            {
                rowIndex = i;
                return true;
            }

            if (ReferenceEquals(button, _stepperRows[i].Increase))
            {
                rowIndex = i;
                onIncrease = true;
                return true;
            }
        }

        return false;
    }

    private bool FocusStepper(int rowIndex, bool onIncrease)
    {
        if (rowIndex < 0 || rowIndex >= _stepperRows.Count)
            return false;

        var target = onIncrease ? _stepperRows[rowIndex].Increase : _stepperRows[rowIndex].Decrease;
        var index = IndexOfSettingsButton(target);
        if (index < 0)
            return false;

        _settingsFocusIndex = index;
        GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
        return true;
    }

    private void TryBeginPointerHold(IPointerSource pointer)
    {
        if (!pointer.WasPrimaryPressed)
            return;

        if (!TryFindStepperUnderPointer(out var focusIndex))
            return;

        _settingsFocusIndex = focusIndex;
        SetFocusZone(FocusZone.Settings, resetIndex: false);
        _settingsEntries[focusIndex].Activate();
        _pointerHoldFocusIndex = focusIndex;
        _pointerRepeatTimer = HeldCommandRepeat.DefaultInitialDelaySeconds;
    }

    private void TickPointerHold(IPointerSource pointer, float elapsedSeconds)
    {
        if (_pointerHoldFocusIndex < 0)
            return;

        if (!pointer.IsPrimaryDown)
        {
            ClearPointerHold();
            return;
        }

        _pointerRepeatTimer -= elapsedSeconds;
        if (_pointerRepeatTimer > 0f)
            return;

        _pointerRepeatTimer = HeldCommandRepeat.DefaultIntervalSeconds;
        _settingsFocusIndex = _pointerHoldFocusIndex;
        GumFocusableButtonList.ApplyFocus(_settingsEntries, ref _settingsFocusIndex);
        _settingsEntries[_pointerHoldFocusIndex].Activate();
    }

    private void ClearPointerHold()
    {
        _pointerHoldFocusIndex = -1;
        _pointerRepeatTimer = 0f;
    }

    private bool TryFindStepperUnderPointer(out int focusIndex)
    {
        focusIndex = -1;
        var over = GumService.Default.Cursor.FrameworkElementOver;
        if (over is null)
            return false;

        foreach (var stepper in _stepperButtons)
        {
            if (!ReferenceEquals(over, stepper) && !ReferenceEquals(over, stepper.Visual))
                continue;

            for (var i = 0; i < _settingsEntries.Count; i++)
            {
                if (!ReferenceEquals(_settingsEntries[i].Button, stepper))
                    continue;
                focusIndex = i;
                return true;
            }
        }

        return false;
    }
}
