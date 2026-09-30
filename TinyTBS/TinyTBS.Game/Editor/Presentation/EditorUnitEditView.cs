using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Editor.Units;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Edit Units/{id}.json: settings scroll (left) + Save/Back menu (right).</summary>
public sealed class EditorUnitEditView
{
    private enum DetailKind
    {
        None,
        Ability,
        Special,
    }

    private readonly EditorTwoColumnFormController _form = new(allowDpadColumnSwitch: false);

    private Panel? _rootPanel;
    private Panel? _settingsHost;
    private Label? _statusLabel;
    private TextBox? _idBox;
    private TextBox? _displayNameBox;
    private TextBox? _spriteBaseBox;
    private TextBox? _spriteMaskBox;
    private TextBox? _newTagBox;
    private readonly EditorChoiceOverlay _choiceOverlay = new();

    private Panel? _detailOverlay;
    private readonly List<(Button Button, Action Activate)> _detailEntries = [];
    private DetailKind _detailKind = DetailKind.None;
    private int _detailEditIndex = -1;
    private TextBox? _detailTagBox;
    private TextBox? _detailAbilityTagsBox;
    private TextBox? _detailSpecialTagBox;
    private Label? _detailTypeLabel;
    private Label? _detailWhenLabel;
    private Label? _detailManhattanRangeLabel;
    private Label? _detailDamageMultiplierLabel;

    private int _detailFocusIndex;
    private EditableUnitDocument _document = EditableUnitDocument.CreateDefault("unit");

    private Label? _tagsSummaryLabel;
    private Label? _attackLabel;
    private Label? _defenceLabel;
    private Label? _maxHealthLabel;
    private Label? _rangeMinLabel;
    private Label? _rangeMaxLabel;
    private Label? _speedLabel;
    private Label? _costLabel;

    public bool IsTextEntryActive =>
        EditorGumTextEntry.IsAnyFocused(
            _idBox,
            _displayNameBox,
            _spriteBaseBox,
            _spriteMaskBox,
            _newTagBox,
            _detailTagBox,
            _detailAbilityTagsBox,
            _detailSpecialTagBox);

    public bool IsOverlayOpen => _choiceOverlay.IsOpen || _detailKind != DetailKind.None;

    public EditableUnitDocument Document => _document;

    public void Build(EditableUnitDocument document, Action onSave, Action onBack)
    {
        Clear();
        _document = document ?? throw new ArgumentNullException(nameof(document));

        var built = EditorTwoColumnFormShell.Build(
            "Edit Unit",
            "LB/RB: columns. L/R: −/+ on steppers. Up/Down: rows. Confirm opens pickers / activates.");
        _rootPanel = built.RootPanel;
        _settingsHost = built.SettingsHost;
        _form.Attach(built);

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        built.MenuHost.AddChild(_statusLabel);

        _form.AddMenuButton(
            built.MenuHost,
            "Save unit",
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
                ? "units." + _document.Id
                : _displayNameBox.Text;
        if (_spriteBaseBox is not null)
            _document.SpriteBase = string.IsNullOrWhiteSpace(_spriteBaseBox.Text) ? null : _spriteBaseBox.Text;
        if (_spriteMaskBox is not null)
            _document.SpriteMask = string.IsNullOrWhiteSpace(_spriteMaskBox.Text) ? null : _spriteMaskBox.Text;
        if (_newTagBox is not null && !string.IsNullOrWhiteSpace(_newTagBox.Text))
            TryAddTag(_newTagBox.Text);
        _document.IsDirty = true;
    }

    public bool TryCloseOverlay()
    {
        if (_choiceOverlay.TryClose())
            return true;
        if (_detailKind != DetailKind.None)
        {
            CloseDetailOverlay(applyChanges: false);
            return true;
        }

        return false;
    }

    public void HandleInput(IGameCommandSource commands, IPointerSource pointer, float elapsedSeconds)
    {
        if (_choiceOverlay.IsOpen)
        {
            _choiceOverlay.HandleInput(commands, elapsedSeconds);
            return;
        }

        if (_detailKind != DetailKind.None)
        {
            HandleDetailInput(commands, elapsedSeconds);
            return;
        }

        if (IsTextEntryActive)
            return;

        _form.HandleInput(commands, elapsedSeconds, pointer);
    }

    public void Clear()
    {
        _choiceOverlay.Close(notifyClosed: false);
        CloseDetailOverlay(applyChanges: false);
        GumService.Default.Root.Children.Clear();
        _form.Clear();
        _rootPanel = null;
        _settingsHost = null;
        _statusLabel = null;
        _idBox = null;
        _displayNameBox = null;
        _spriteBaseBox = null;
        _spriteMaskBox = null;
        _newTagBox = null;
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
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Unit Id (Units/*.json)", _document.Id, out _idBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Display name key", _document.DisplayNameKey, out _displayNameBox);

        AddSectionLabel("Movement");
        AddSettingsButton(MovementButtonCaption(), OpenMovementChoice);

        AddSectionLabel("Tags");
        _tagsSummaryLabel = new Label { Text = TagsCaption() };
        GumUiLayout.FillParentWidth(_tagsSummaryLabel);
        _settingsHost.AddChild(_tagsSummaryLabel);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "New tag", string.Empty, out _newTagBox);
        AddSettingsButton("Add tag", CommitNewTag);
        foreach (var tag in _document.Tags.ToList())
        {
            var captured = tag;
            AddSettingsButton("Tag: " + captured, () => OpenTagChoice(captured));
        }

        AddSectionLabel("Stats");
        _attackLabel = AddStatLabel("Attack: " + _document.Attack);
        AddStatStepper(() => AdjustAttack(-1), () => AdjustAttack(+1));
        _defenceLabel = AddStatLabel("Defence: " + _document.Defence);
        AddStatStepper(() => AdjustDefence(-1), () => AdjustDefence(+1));
        _maxHealthLabel = AddStatLabel("Max health: " + _document.MaxHealth);
        AddStatStepper(() => AdjustMaxHealth(-5), () => AdjustMaxHealth(+5));
        _rangeMinLabel = AddStatLabel("Range min: " + _document.AttackRangeMin);
        AddStatStepper(() => AdjustRangeMin(-1), () => AdjustRangeMin(+1));
        _rangeMaxLabel = AddStatLabel("Range max: " + _document.AttackRangeMax);
        AddStatStepper(() => AdjustRangeMax(-1), () => AdjustRangeMax(+1));
        _speedLabel = AddStatLabel("Speed: " + _document.Speed);
        AddStatStepper(() => AdjustSpeed(-1), () => AdjustSpeed(+1));
        _costLabel = AddStatLabel("Cost: " + _document.Cost);
        AddStatStepper(() => AdjustCost(-10), () => AdjustCost(+10));

        AddSectionLabel("Flags");
        AddSettingsButton(RecruitableCaption(), ToggleRecruitable);
        AddSettingsButton(GravestoneCaption(), ToggleGravestone);

        AddSectionLabel("Sprites");
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Sprite base path", _document.SpriteBase ?? string.Empty, out _spriteBaseBox);
        EditorTwoColumnFormShell.AddTextField(_settingsHost, "Sprite mask path", _document.SpriteMask ?? string.Empty, out _spriteMaskBox);

        AddSectionLabel("Abilities");
        for (var index = 0; index < _document.Abilities.Count; index++)
        {
            var capturedIndex = index;
            var ability = _document.Abilities[index];
            AddSettingsButton(ability.SummaryLine, () => OpenAbilityChoice(capturedIndex));
        }

        AddSettingsButton("Add ability…", OpenAddAbilityChoice);

        AddSectionLabel("Special coefficients");
        for (var index = 0; index < _document.SpecialCoefficients.Count; index++)
        {
            var capturedIndex = index;
            var coefficient = _document.SpecialCoefficients[index];
            AddSettingsButton(coefficient.SummaryLine, () => OpenSpecialChoice(capturedIndex));
        }

        AddSettingsButton("Add special coefficient", AddDefaultSpecial);
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

    private void HandleDetailInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (IsTextEntryActive)
            return;

        if (commands.WasPressed(GameCommand.Cancel)
            || commands.WasPressed(GameCommand.Back)
            || commands.WasPressed(GameCommand.Info)
            || commands.WasPressed(GameCommand.Pause))
        {
            CloseDetailOverlay(applyChanges: false);
            return;
        }

        if (_detailEntries.Count == 0)
            return;

        if (IsDetailStepperFocused())
        {
            var horizontal = _form.NavigateRepeat.TryGetHorizontalDelta(commands, elapsedSeconds);
            if (horizontal != 0)
            {
                TryMoveDetailStepper(horizontal);
                return;
            }
        }

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _detailEntries,
            ref _detailFocusIndex,
            _form.NavigateRepeat,
            elapsedSeconds);
    }

    private void CommitNewTag()
    {
        // Apply before clearing — otherwise RebuildFromDocument sees an empty box.
        RebuildFromDocument();
    }

    private void TryAddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;
        if (_document.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            return;
        _document.Tags.Add(tag);
        _document.IsDirty = true;
        if (_tagsSummaryLabel is not null)
            _tagsSummaryLabel.Text = TagsCaption();
    }

    private void OpenTagChoice(string tag)
    {
        if (_rootPanel is null)
            return;

        _choiceOverlay.Open(
            _rootPanel,
            "Tag: " + tag,
            [
                ("Remove", () =>
                {
                    _document.Tags.RemoveAll(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase));
                    _document.IsDirty = true;
                    RebuildFromDocument();
                }),
            ],
            onClosed: () => _form.FocusSettings());
    }

    private void OpenAbilityChoice(int index)
    {
        if (_rootPanel is null || index < 0 || index >= _document.Abilities.Count)
            return;

        _choiceOverlay.Open(
            _rootPanel,
            "Ability",
            [
                ("Edit", () => OpenAbilityDetail(index)),
                ("Delete", () =>
                {
                    _document.Abilities.RemoveAt(index);
                    _document.IsDirty = true;
                    RebuildFromDocument();
                }),
                ("Move Up", () => MoveAbility(index, -1)),
                ("Move Down", () => MoveAbility(index, +1)),
            ],
            onClosed: () => _form.FocusSettings());
    }

    private void OpenSpecialChoice(int index)
    {
        if (_rootPanel is null || index < 0 || index >= _document.SpecialCoefficients.Count)
            return;

        _choiceOverlay.Open(
            _rootPanel,
            "Special coefficient",
            [
                ("Edit", () => OpenSpecialDetail(index)),
                ("Delete", () =>
                {
                    _document.SpecialCoefficients.RemoveAt(index);
                    _document.IsDirty = true;
                    RebuildFromDocument();
                }),
                ("Move Up", () => MoveSpecial(index, -1)),
                ("Move Down", () => MoveSpecial(index, +1)),
            ],
            onClosed: () => _form.FocusSettings());
    }

    private void MoveAbility(int index, int delta)
    {
        var target = index + delta;
        if (target < 0 || target >= _document.Abilities.Count)
            return;
        (_document.Abilities[index], _document.Abilities[target]) =
            (_document.Abilities[target], _document.Abilities[index]);
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void MoveSpecial(int index, int delta)
    {
        var target = index + delta;
        if (target < 0 || target >= _document.SpecialCoefficients.Count)
            return;
        (_document.SpecialCoefficients[index], _document.SpecialCoefficients[target]) =
            (_document.SpecialCoefficients[target], _document.SpecialCoefficients[index]);
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void OpenMovementChoice()
    {
        if (_rootPanel is null)
            return;

        var actions = new List<(string Label, Action Activate)>();
        foreach (var movementClass in MovementClassIds.All)
        {
            var captured = movementClass;
            actions.Add((MovementCaption(captured), () =>
            {
                _document.MovementClass = captured;
                _document.IsDirty = true;
                RebuildFromDocument();
            }));
        }

        _choiceOverlay.Open(
            _rootPanel,
            "Movement class",
            actions,
            onClosed: () => _form.FocusSettings());
    }

    private void OpenAddAbilityChoice()
    {
        if (_rootPanel is null)
            return;

        var actions = new List<(string Label, Action Activate)>();
        foreach (var abilityType in UnitAbilityTypes.All)
        {
            var captured = abilityType;
            actions.Add((captured, () =>
            {
                _document.Abilities.Add(new EditableUnitAbility { Type = captured });
                _document.IsDirty = true;
                RebuildFromDocument();
            }));
        }

        _choiceOverlay.Open(
            _rootPanel,
            "Add ability",
            actions,
            onClosed: () => _form.FocusSettings());
    }

    private void OpenAbilityTypeChoice(EditableUnitAbility ability)
    {
        if (_rootPanel is null)
            return;

        var actions = new List<(string Label, Action Activate)>();
        foreach (var abilityType in UnitAbilityTypes.All)
        {
            var captured = abilityType;
            actions.Add((captured, () =>
            {
                ability.Type = captured;
                _document.IsDirty = true;
                if (_detailTypeLabel is not null)
                    _detailTypeLabel.Text = "Type: " + ability.Type;
            }));
        }

        _choiceOverlay.Open(
            _rootPanel,
            "Ability type",
            actions,
            onClosed: RestoreDetailFocus);
    }

    private void OpenSpecialWhenChoice(EditableUnitSpecialCoefficient coefficient)
    {
        if (_rootPanel is null)
            return;

        _choiceOverlay.Open(
            _rootPanel,
            "When condition",
            [
                ("Default", () => ApplySpecialWhenDefault(coefficient)),
                ("Target has tag", () => ApplySpecialWhenTargetHasTag(coefficient)),
                ("Manhattan range", () => ApplySpecialWhenManhattanRange(coefficient)),
            ],
            onClosed: RestoreDetailFocus);
    }

    private void ApplySpecialWhenDefault(EditableUnitSpecialCoefficient coefficient)
    {
        coefficient.WhenDefault = true;
        coefficient.TargetHasTag = null;
        coefficient.ManhattanRange = null;
        RefreshSpecialWhenUi(coefficient);
    }

    private void ApplySpecialWhenTargetHasTag(EditableUnitSpecialCoefficient coefficient)
    {
        coefficient.WhenDefault = false;
        coefficient.TargetHasTag = "infantry";
        coefficient.ManhattanRange = null;
        RefreshSpecialWhenUi(coefficient);
    }

    private void ApplySpecialWhenManhattanRange(EditableUnitSpecialCoefficient coefficient)
    {
        coefficient.WhenDefault = false;
        coefficient.TargetHasTag = null;
        coefficient.ManhattanRange = coefficient.ManhattanRange is > 0 ? coefficient.ManhattanRange : 1;
        RefreshSpecialWhenUi(coefficient);
    }

    private void RefreshSpecialWhenUi(EditableUnitSpecialCoefficient coefficient)
    {
        _document.IsDirty = true;
        if (_detailWhenLabel is not null)
            _detailWhenLabel.Text = WhenCaption(coefficient);
        if (_detailSpecialTagBox is not null)
            _detailSpecialTagBox.Text = coefficient.TargetHasTag ?? string.Empty;
        if (_detailManhattanRangeLabel is not null)
            _detailManhattanRangeLabel.Text = "Manhattan range: " + ManhattanRangeStepperCaption(coefficient);
    }

    private void AddDefaultSpecial()
    {
        _document.SpecialCoefficients.Add(new EditableUnitSpecialCoefficient
        {
            WhenDefault = true,
            Multiply = 1.0,
        });
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void OpenAbilityDetail(int index)
    {
        if (_rootPanel is null || index < 0 || index >= _document.Abilities.Count)
            return;

        CloseDetailOverlay(applyChanges: false);
        _detailKind = DetailKind.Ability;
        _detailEditIndex = index;
        _detailEntries.Clear();

        _detailOverlay = new Panel();
        _detailOverlay.Dock(Dock.Fill);
        _rootPanel.AddChild(_detailOverlay);

        var scrim = new Panel();
        scrim.Dock(Dock.Fill);
        _detailOverlay.AddChild(scrim);
        GumUiLayout.AddSolidBackground(scrim, UiColors.MenuDetailScrim);

        var card = new Panel();
        GumUiLayout.CenterInParent(card, xPercent: 50f, yPercent: 50f);
        GumUiLayout.SetBoundedWidth(card, maxPixels: 520f, parentPercent: 94f);
        card.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _detailOverlay.AddChild(card);
        GumUiLayout.AddSolidBackground(card, UiColors.EditorPanel);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 8f, widthPercent: 92f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        card.AddChild(stack);
        GumUiLayout.AddVerticalSpacer(stack, 12f);

        var title = new Label { Text = "Edit ability" };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        var ability = _document.Abilities[index];
        _detailTypeLabel = new Label { Text = "Type: " + ability.Type };
        GumUiLayout.FillParentWidth(_detailTypeLabel);
        stack.AddChild(_detailTypeLabel);
        AddDetailButton(stack, "Choose type…", () => OpenAbilityTypeChoice(ability));

        AddDetailStepper(stack, "Amount", () => OptionalCaption(ability.Amount), () => AdjustAbilityOptional(ability, field: 0, -1), () => AdjustAbilityOptional(ability, field: 0, +1));
        AddDetailStepper(stack, "Min range", () => OptionalCaption(ability.MinRange), () => AdjustAbilityOptional(ability, field: 1, -1), () => AdjustAbilityOptional(ability, field: 1, +1));
        AddDetailStepper(stack, "Value", () => OptionalCaption(ability.Value), () => AdjustAbilityOptional(ability, field: 2, -1), () => AdjustAbilityOptional(ability, field: 2, +1));
        AddDetailStepper(stack, "Radius", () => OptionalCaption(ability.Radius), () => AdjustAbilityOptional(ability, field: 3, -1), () => AdjustAbilityOptional(ability, field: 3, +1));

        var tagsLabel = new Label { Text = "Tags (comma-separated)" };
        GumUiLayout.FillParentWidth(tagsLabel);
        stack.AddChild(tagsLabel);
        _detailAbilityTagsBox = new TextBox { Text = string.Join(", ", ability.Tags) };
        GumUiLayout.FillParentWidth(_detailAbilityTagsBox);
        GumUiLayout.SetAbsoluteHeight(_detailAbilityTagsBox, EditorTwoColumnFormShell.DefaultTextFieldHeight);
        EditorTextFieldStyle.Apply(_detailAbilityTagsBox);
        stack.AddChild(_detailAbilityTagsBox);

        AddDetailButton(stack, "Done", () => CloseDetailOverlay(applyChanges: true));
        GumUiLayout.AddVerticalSpacer(stack, 12f);

        _detailFocusIndex = 0;
        if (_detailEntries.Count > 0)
            GumFocusableButtonList.ApplyFocus(_detailEntries, ref _detailFocusIndex);
    }

    private void OpenSpecialDetail(int index)
    {
        if (_rootPanel is null || index < 0 || index >= _document.SpecialCoefficients.Count)
            return;

        CloseDetailOverlay(applyChanges: false);
        _detailKind = DetailKind.Special;
        _detailEditIndex = index;
        _detailEntries.Clear();

        _detailOverlay = new Panel();
        _detailOverlay.Dock(Dock.Fill);
        _rootPanel.AddChild(_detailOverlay);

        var scrim = new Panel();
        scrim.Dock(Dock.Fill);
        _detailOverlay.AddChild(scrim);
        GumUiLayout.AddSolidBackground(scrim, UiColors.MenuDetailScrim);

        var card = new Panel();
        GumUiLayout.CenterInParent(card, xPercent: 50f, yPercent: 50f);
        GumUiLayout.SetBoundedWidth(card, maxPixels: 520f, parentPercent: 94f);
        card.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _detailOverlay.AddChild(card);
        GumUiLayout.AddSolidBackground(card, UiColors.EditorPanel);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 8f, widthPercent: 92f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        card.AddChild(stack);
        GumUiLayout.AddVerticalSpacer(stack, 12f);

        var title = new Label { Text = "Edit damage special" };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        var whenHint = new Label
        {
            Text = "Outgoing damage is multiplied when the when-condition matches.",
        };
        GumUiLayout.FillParentWidth(whenHint);
        stack.AddChild(whenHint);

        var coefficient = _document.SpecialCoefficients[index];
        if (!coefficient.WhenDefault && string.IsNullOrWhiteSpace(coefficient.TargetHasTag))
            coefficient.ManhattanRange ??= 1;

        _detailWhenLabel = new Label { Text = WhenCaption(coefficient) };
        GumUiLayout.FillParentWidth(_detailWhenLabel);
        stack.AddChild(_detailWhenLabel);
        AddDetailButton(stack, "Choose when…", () => OpenSpecialWhenChoice(coefficient));

        var tagCaption = new Label { Text = "Target tag (when = targetHasTag)" };
        GumUiLayout.FillParentWidth(tagCaption);
        stack.AddChild(tagCaption);
        _detailSpecialTagBox = new TextBox { Text = coefficient.TargetHasTag ?? string.Empty };
        GumUiLayout.FillParentWidth(_detailSpecialTagBox);
        GumUiLayout.SetAbsoluteHeight(_detailSpecialTagBox, EditorTwoColumnFormShell.DefaultTextFieldHeight);
        EditorTextFieldStyle.Apply(_detailSpecialTagBox);
        stack.AddChild(_detailSpecialTagBox);

        AddDetailStepper(
            stack,
            "Manhattan range",
            () => ManhattanRangeStepperCaption(coefficient),
            () => AdjustSpecialRange(coefficient, -1),
            () => AdjustSpecialRange(coefficient, +1),
            out _detailManhattanRangeLabel);

        var multiplierHint = new Label
        {
            Text = "Damage multiplier applies to outgoing damage when the when-condition matches.",
        };
        GumUiLayout.FillParentWidth(multiplierHint);
        stack.AddChild(multiplierHint);

        AddDetailStepper(
            stack,
            "Damage multiplier",
            () => DamageMultiplierCaption(coefficient),
            () => AdjustMultiply(coefficient, -0.05),
            () => AdjustMultiply(coefficient, +0.05),
            out _detailDamageMultiplierLabel);

        AddDetailButton(stack, "Done", () => CloseDetailOverlay(applyChanges: true));
        GumUiLayout.AddVerticalSpacer(stack, 12f);

        _detailFocusIndex = 0;
        if (_detailEntries.Count > 0)
            GumFocusableButtonList.ApplyFocus(_detailEntries, ref _detailFocusIndex);
    }

    private void CloseDetailOverlay(bool applyChanges)
    {
        if (_detailKind == DetailKind.None || _detailOverlay is null || _rootPanel is null)
        {
            _detailKind = DetailKind.None;
            _detailEditIndex = -1;
            _detailEntries.Clear();
            _detailOverlay = null;
            return;
        }

        if (applyChanges && _detailEditIndex >= 0)
        {
            if (_detailKind == DetailKind.Ability && _detailEditIndex < _document.Abilities.Count)
            {
                var ability = _document.Abilities[_detailEditIndex];
                if (_detailAbilityTagsBox is not null)
                {
                    ability.Tags = (_detailAbilityTagsBox.Text ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                NormalizeAbilityOptionalFields(ability);
            }
            else if (_detailKind == DetailKind.Special && _detailEditIndex < _document.SpecialCoefficients.Count)
            {
                var coefficient = _document.SpecialCoefficients[_detailEditIndex];
                if (_detailSpecialTagBox is not null && !coefficient.WhenDefault && coefficient.ManhattanRange is null)
                    coefficient.TargetHasTag = string.IsNullOrWhiteSpace(_detailSpecialTagBox.Text)
                        ? null
                        : _detailSpecialTagBox.Text;
            }

            _document.IsDirty = true;
            RebuildFromDocument();
        }

        for (var i = _rootPanel.Visual.Children.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_rootPanel.Visual.Children[i], _detailOverlay.Visual))
            {
                _rootPanel.Visual.Children.RemoveAt(i);
                break;
            }
        }

        _detailOverlay.Visual.Parent = null;
        _detailOverlay = null;
        _detailKind = DetailKind.None;
        _detailEditIndex = -1;
        _detailEntries.Clear();
        _detailTagBox = null;
        _detailAbilityTagsBox = null;
        _detailSpecialTagBox = null;
        _detailTypeLabel = null;
        _detailWhenLabel = null;
        _detailManhattanRangeLabel = null;
        _detailDamageMultiplierLabel = null;
        _form.FocusSettings();
    }

    private void RestoreDetailFocus()
    {
        if (_detailEntries.Count > 0)
            GumFocusableButtonList.ApplyFocus(_detailEntries, ref _detailFocusIndex);
    }

    private void AddDetailButton(Panel stack, string text, Action onClick)
    {
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) => onClick();
        stack.AddChild(button);
        _detailEntries.Add((button, onClick));
    }

    private void AddDetailStepper(
        Panel stack,
        string caption,
        Func<string> valueCaption,
        Action onDecrease,
        Action onIncrease)
    {
        AddDetailStepper(stack, caption, valueCaption, onDecrease, onIncrease, out _);
    }

    private void AddDetailStepper(
        Panel stack,
        string caption,
        Func<string> valueCaption,
        Action onDecrease,
        Action onIncrease,
        out Label valueLabel)
    {
        valueLabel = new Label { Text = caption + ": " + valueCaption() };
        GumUiLayout.FillParentWidth(valueLabel);
        stack.AddChild(valueLabel);
        var label = valueLabel;

        var row = new Panel();
        GumUiLayout.FillParentWidth(row);
        row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.Visual.StackSpacing = 8f;
        stack.AddChild(row);

        void RefreshLabel() => label.Text = caption + ": " + valueCaption();

        var decrease = new Button { Text = "-" };
        GumUiLayout.SetAbsoluteWidth(decrease, 72f);
        decrease.Click += (_, _) =>
        {
            onDecrease();
            RefreshLabel();
        };
        row.AddChild(decrease);

        var increase = new Button { Text = "+" };
        GumUiLayout.SetAbsoluteWidth(increase, 72f);
        increase.Click += (_, _) =>
        {
            onIncrease();
            RefreshLabel();
        };
        row.AddChild(increase);

        _detailEntries.Add((decrease, () =>
        {
            onDecrease();
            RefreshLabel();
        }));
        _detailEntries.Add((increase, () =>
        {
            onIncrease();
            RefreshLabel();
        }));
    }

    private bool IsDetailStepperFocused()
    {
        if (_detailFocusIndex < 0 || _detailFocusIndex >= _detailEntries.Count)
            return false;
        var text = _detailEntries[_detailFocusIndex].Button.Text;
        return text is "-" or "+";
    }

    private void TryMoveDetailStepper(int horizontalDelta)
    {
        if (_detailFocusIndex < 0 || _detailFocusIndex >= _detailEntries.Count)
            return;
        var target = _detailFocusIndex + (horizontalDelta > 0 ? 1 : -1);
        if (target < 0 || target >= _detailEntries.Count)
            return;
        _detailFocusIndex = target;
        GumFocusableButtonList.ApplyFocus(_detailEntries, ref _detailFocusIndex);
    }

    private static void AdjustAbilityOptional(EditableUnitAbility ability, int field, int delta)
    {
        var current = field switch
        {
            1 => ability.MinRange ?? 0,
            2 => ability.Value ?? 0,
            3 => ability.Radius ?? 0,
            _ => ability.Amount ?? 0,
        };
        current = Math.Max(0, current + delta);
        var next = current == 0 ? (int?)null : current;
        switch (field)
        {
            case 1:
                ability.MinRange = next;
                break;
            case 2:
                ability.Value = next;
                break;
            case 3:
                ability.Radius = next;
                break;
            default:
                ability.Amount = next;
                break;
        }
    }

    private static void NormalizeAbilityOptionalFields(EditableUnitAbility ability)
    {
        if (ability.Amount is 0)
            ability.Amount = null;
        if (ability.MinRange is 0)
            ability.MinRange = null;
        if (ability.Value is 0)
            ability.Value = null;
        if (ability.Radius is 0)
            ability.Radius = null;
    }

    private void AdjustSpecialRange(EditableUnitSpecialCoefficient coefficient, int delta)
    {
        if (coefficient.WhenDefault || !string.IsNullOrWhiteSpace(coefficient.TargetHasTag))
            return;
        var current = coefficient.ManhattanRange ?? 1;
        current = Math.Max(1, current + delta);
        coefficient.ManhattanRange = current;
        _document.IsDirty = true;
        if (_detailManhattanRangeLabel is not null)
            _detailManhattanRangeLabel.Text = "Manhattan range: " + ManhattanRangeStepperCaption(coefficient);
    }

    private void AdjustMultiply(EditableUnitSpecialCoefficient coefficient, double delta)
    {
        coefficient.Multiply = Math.Max(0, Math.Round((coefficient.Multiply + delta) * 100) / 100.0);
        _document.IsDirty = true;
        if (_detailDamageMultiplierLabel is not null)
            _detailDamageMultiplierLabel.Text = "Damage multiplier: " + DamageMultiplierCaption(coefficient);
    }

    private void ToggleRecruitable()
    {
        _document.Recruitable = !_document.Recruitable;
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void ToggleGravestone()
    {
        _document.LeavesGravestone = !_document.LeavesGravestone;
        _document.IsDirty = true;
        RebuildFromDocument();
    }

    private void AdjustAttack(int delta)
    {
        _document.Attack = Math.Clamp(_document.Attack + delta, 0, 999);
        _document.IsDirty = true;
        if (_attackLabel is not null)
            _attackLabel.Text = "Attack: " + _document.Attack;
    }

    private void AdjustDefence(int delta)
    {
        _document.Defence = Math.Clamp(_document.Defence + delta, 0, 99);
        _document.IsDirty = true;
        if (_defenceLabel is not null)
            _defenceLabel.Text = "Defence: " + _document.Defence;
    }

    private void AdjustMaxHealth(int delta)
    {
        _document.MaxHealth = Math.Clamp(_document.MaxHealth + delta, 1, 9999);
        _document.IsDirty = true;
        if (_maxHealthLabel is not null)
            _maxHealthLabel.Text = "Max health: " + _document.MaxHealth;
    }

    private void AdjustRangeMin(int delta)
    {
        _document.AttackRangeMin = Math.Clamp(_document.AttackRangeMin + delta, 0, 99);
        if (_document.AttackRangeMax < _document.AttackRangeMin)
            _document.AttackRangeMax = _document.AttackRangeMin;
        _document.IsDirty = true;
        if (_rangeMinLabel is not null)
            _rangeMinLabel.Text = "Range min: " + _document.AttackRangeMin;
        if (_rangeMaxLabel is not null)
            _rangeMaxLabel.Text = "Range max: " + _document.AttackRangeMax;
    }

    private void AdjustRangeMax(int delta)
    {
        _document.AttackRangeMax = Math.Clamp(_document.AttackRangeMax + delta, _document.AttackRangeMin, 99);
        _document.IsDirty = true;
        if (_rangeMaxLabel is not null)
            _rangeMaxLabel.Text = "Range max: " + _document.AttackRangeMax;
    }

    private void AdjustSpeed(int delta)
    {
        _document.Speed = Math.Clamp(_document.Speed + delta, 0, 99);
        _document.IsDirty = true;
        if (_speedLabel is not null)
            _speedLabel.Text = "Speed: " + _document.Speed;
    }

    private void AdjustCost(int delta)
    {
        _document.Cost = Math.Max(0, _document.Cost + delta);
        _document.IsDirty = true;
        if (_costLabel is not null)
            _costLabel.Text = "Cost: " + _document.Cost;
    }

    private static string OptionalCaption(int? value) => value?.ToString() ?? "—";

    private static string MovementCaption(string movementClass) => "Movement: " + movementClass;

    private string MovementButtonCaption() => MovementCaption(_document.MovementClass) + "…";

    private static string ManhattanRangeStepperCaption(EditableUnitSpecialCoefficient coefficient) =>
        coefficient.ManhattanRange is int range ? range.ToString() : "—";

    private static string DamageMultiplierCaption(EditableUnitSpecialCoefficient coefficient) =>
        coefficient.Multiply.ToString("0.##");

    private string TagsCaption() =>
        _document.Tags.Count == 0 ? "Tags: (none)" : "Tags: " + string.Join(", ", _document.Tags);

    private string RecruitableCaption() => "Recruitable: " + (_document.Recruitable ? "On" : "Off");

    private string GravestoneCaption() => "Leaves gravestone: " + (_document.LeavesGravestone ? "On" : "Off");

    private static string WhenCaption(EditableUnitSpecialCoefficient coefficient)
    {
        if (coefficient.WhenDefault)
            return "When: default";
        if (!string.IsNullOrWhiteSpace(coefficient.TargetHasTag))
            return "When: targetHasTag (" + coefficient.TargetHasTag + ")";
        if (coefficient.ManhattanRange is int range)
            return "When: manhattanRange (" + range + ")";
        return "When: ?";
    }
}
