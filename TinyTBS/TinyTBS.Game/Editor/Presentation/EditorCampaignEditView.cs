using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Edit linear campaign chapters: available levels (left) + ordered chapters (right).</summary>
public sealed class EditorCampaignEditView
{
    private enum FocusZone
    {
        Available = 0,
        Chapters = 1,
        Footer = 2,
    }

    private const float TextFieldHeight = 36f;
    private const float StackSpacing = 6f;
    private const float LevelListMinHeight = 220f;

    private Panel? _rootPanel;
    private Panel? _availableHost;
    private Panel? _chaptersHost;
    private ScrollViewer? _availableScroll;
    private ScrollViewer? _chaptersScroll;
    private Label? _statusLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private readonly List<(Button Button, Action Activate)> _availableEntries = [];
    private readonly List<(Button Button, Action Activate)> _chapterEntries = [];
    private readonly List<(Button Button, Action Activate)> _footerEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private readonly EditorChoiceOverlay _choiceOverlay = new();
    private readonly List<string> _chapters = [];
    private IReadOnlyList<string> _availableLevels = [];
    private Action? _onSave;
    private Action? _onBack;
    private FocusZone _focusZone = FocusZone.Available;
    private int _availableFocusIndex;
    private int _chapterFocusIndex;
    private int _footerFocusIndex;
    private FocusZone _lastColumnZone = FocusZone.Available;

    public bool IsTextEntryActive =>
        _idBox is { IsFocused: true } || _titleBox is { IsFocused: true };

    public bool IsChoiceOpen => _choiceOverlay.IsOpen;

    public string CampaignId =>
        string.IsNullOrWhiteSpace(_idBox?.Text) ? "campaign" : _idBox!.Text.Trim();

    public string CampaignTitle =>
        string.IsNullOrWhiteSpace(_titleBox?.Text) ? CampaignId : _titleBox!.Text.Trim();

    public IReadOnlyList<string> Chapters => _chapters;

    public void Build(
        string campaignId,
        string title,
        IReadOnlyList<string> chapters,
        IReadOnlyList<string> availableLevels,
        Action onSave,
        Action onBack)
    {
        Clear();
        _onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));
        _onBack = onBack ?? throw new ArgumentNullException(nameof(onBack));
        _chapters.Clear();
        _chapters.AddRange(chapters.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()));
        _availableLevels = availableLevels ?? [];

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var body = new Panel();
        body.Dock(Dock.Fill);
        _rootPanel.AddChild(body);

        var shell = new Panel();
        GumUiLayout.CenterHorizontallyInParent(shell);
        shell.Visual.Y = 12f;
        GumUiLayout.SetBoundedWidth(shell, maxPixels: 760f, parentPercent: 96f);
        var canvasHeight = Math.Max(320f, GumService.Default.CanvasHeight);
        GumUiLayout.SetBoundedHeight(shell, maxPixels: canvasHeight - 24f, parentPercent: 94f);
        body.AddChild(shell);
        GumUiLayout.AddSolidBackground(shell, EditorUiColors.Panel);

        var listScroll = new ScrollViewer();
        listScroll.Dock(Dock.Fill);
        listScroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        listScroll.InnerPanel.Width = 0;
        listScroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        listScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        GumScrollViewerChrome.DisableScrollChromeFocus(listScroll);
        shell.AddChild(listScroll);

        var listPanel = GumUiLayout.CreateVerticalStackPanel(spacing: StackSpacing, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(listPanel);
        listScroll.AddChild(listPanel);
        GumUiLayout.AddVerticalSpacer(listPanel, 10f);

        var heading = new Label { Text = "Edit Campaign" };
        GumUiLayout.FillParentWidth(heading);
        listPanel.AddChild(heading);

        var hint = new Label
        {
            Text = "Left/Right or LB/RB (LT/RT): switch panels. Up/Down inside a panel. Confirm opens menu.",
        };
        GumUiLayout.FillParentWidth(hint);
        listPanel.AddChild(hint);

        AddTextField(listPanel, "Campaign Id", campaignId, out _idBox);
        AddTextField(listPanel, "Title", title, out _titleBox);

        var listHeight = Math.Clamp(canvasHeight * 0.42f, LevelListMinHeight, 420f);
        var columns = new Panel();
        GumUiLayout.FillParentWidth(columns);
        columns.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        columns.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        columns.Visual.StackSpacing = 10f;
        listPanel.AddChild(columns);

        var leftColumn = CreateColumn(
            columns,
            "Available levels",
            listHeight,
            out _availableHost,
            out _availableScroll);
        var rightColumn = CreateColumn(
            columns,
            "Campaign chapters (order)",
            listHeight,
            out _chaptersHost,
            out _chaptersScroll);
        GumUiLayout.SetWidthPercent(leftColumn, 49f);
        GumUiLayout.SetWidthPercent(rightColumn, 49f);

        _statusLabel = new Label { Text = StatusCaption() };
        GumUiLayout.FillParentWidth(_statusLabel);
        listPanel.AddChild(_statusLabel);

        var footerHost = new Panel();
        GumUiLayout.FillParentWidth(footerHost);
        footerHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        listPanel.AddChild(footerHost);

        var saveButton = new Button { Text = "Save" };
        saveButton.Click += (_, _) => _onSave!();
        var backButton = new Button { Text = "Back" };
        backButton.Click += (_, _) => _onBack!();
        GumUiLayout.LayoutAdaptiveButtonRows(
            footerHost,
            [saveButton, backButton],
            availableWidth: 700f,
            spacing: 8f,
            minButtonWidth: 120f,
            preferredButtonWidth: 220f);
        _footerEntries.Add((saveButton, () => _onSave!()));
        _footerEntries.Add((backButton, () => _onBack!()));
        GumUiLayout.AddVerticalSpacer(listPanel, 12f);

        RebuildLevelLists();
        SetFocusZone(FocusZone.Available, resetIndex: true);
    }

    public void SyncStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text ?? string.Empty;
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

        if (_focusZone == FocusZone.Footer)
        {
            HandleFooterInput(commands, elapsedSeconds);
            return;
        }

        HandleColumnInput(commands, elapsedSeconds);
    }

    public bool TryCloseChoice() => _choiceOverlay.TryClose();

    public void Clear()
    {
        _choiceOverlay.Close(notifyClosed: false);
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _availableHost = null;
        _chaptersHost = null;
        _availableScroll = null;
        _chaptersScroll = null;
        _statusLabel = null;
        _idBox = null;
        _titleBox = null;
        _availableEntries.Clear();
        _chapterEntries.Clear();
        _footerEntries.Clear();
        _navigateRepeat.Reset();
        _focusZone = FocusZone.Available;
        _availableFocusIndex = 0;
        _chapterFocusIndex = 0;
        _footerFocusIndex = 0;
        _lastColumnZone = FocusZone.Available;
        _chapters.Clear();
        _availableLevels = [];
        _onSave = null;
        _onBack = null;
    }

    private void HandleColumnInput(IGameCommandSource commands, float elapsedSeconds)
    {
        var entries = CurrentColumnEntries();
        if (entries.Count == 0)
        {
            // Empty column: allow Down to footer or horizontal panel switch already handled.
            if (commands.WasPressed(GameCommand.NavigateDown))
                SetFocusZone(FocusZone.Footer, resetIndex: true);
            return;
        }

        ref var focusIndex = ref CurrentColumnFocusIndex();
        var before = focusIndex;
        var result = GumFocusableButtonList.HandleVerticalInput(
            commands,
            entries,
            ref focusIndex,
            _navigateRepeat,
            elapsedSeconds);

        if (result == GumFocusListResult.Navigated)
        {
            // Down past the last row → Save/Back footer.
            if (before == focusIndex && before >= entries.Count - 1)
                SetFocusZone(FocusZone.Footer, resetIndex: true);
            else
                EnsureColumnRowVisible();
            return;
        }

        if (result == GumFocusListResult.Activated)
            return;
    }

    private void HandleFooterInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_footerEntries.Count == 0)
            return;

        // Up returns to the last used column panel.
        if (commands.WasPressed(GameCommand.NavigateUp))
        {
            SetFocusZone(_lastColumnZone, resetIndex: false);
            return;
        }

        // Horizontal Save ↔ Back; Confirm activates.
        GumFocusableButtonList.HandleHorizontalInput(
            commands,
            _footerEntries,
            ref _footerFocusIndex,
            _navigateRepeat,
            elapsedSeconds);
    }

    private bool TrySwitchColumn(IGameCommandSource commands)
    {
        var delta = 0;
        if (commands.WasPressed(GameCommand.NavigateLeft)
            || commands.WasPressed(GameCommand.FocusPreviousRegion)
            || commands.WasPressed(GameCommand.ZoomOut))
        {
            delta = -1;
        }
        else if (commands.WasPressed(GameCommand.NavigateRight)
            || commands.WasPressed(GameCommand.FocusNextRegion)
            || commands.WasPressed(GameCommand.ZoomIn))
        {
            delta = +1;
        }

        if (delta == 0)
            return false;

        if (_focusZone == FocusZone.Footer)
        {
            // On footer, Left/Right already move Save/Back — leave them to HandleFooterInput.
            // Shoulders / triggers still jump to a column.
            if (commands.WasPressed(GameCommand.NavigateLeft)
                || commands.WasPressed(GameCommand.NavigateRight))
            {
                return false;
            }

            SetFocusZone(delta < 0 ? FocusZone.Available : FocusZone.Chapters, resetIndex: false);
            return true;
        }

        var next = _focusZone == FocusZone.Available ? FocusZone.Chapters : FocusZone.Available;
        if ((_focusZone == FocusZone.Available && delta < 0)
            || (_focusZone == FocusZone.Chapters && delta > 0))
        {
            // Stay on edge column.
            return true;
        }

        SetFocusZone(next, resetIndex: false);
        return true;
    }

    private void SetFocusZone(FocusZone zone, bool resetIndex)
    {
        _focusZone = zone;
        if (zone is FocusZone.Available or FocusZone.Chapters)
            _lastColumnZone = zone;

        if (zone == FocusZone.Available)
        {
            if (resetIndex || _availableEntries.Count == 0)
                _availableFocusIndex = 0;
            else
                _availableFocusIndex = Math.Clamp(_availableFocusIndex, 0, _availableEntries.Count - 1);

            if (_availableEntries.Count > 0)
            {
                GumFocusableButtonList.ClearFocus(_chapterEntries);
                GumFocusableButtonList.ClearFocus(_footerEntries);
                GumFocusableButtonList.ApplyFocus(_availableEntries, ref _availableFocusIndex);
                EnsureColumnRowVisible();
            }
        }
        else if (zone == FocusZone.Chapters)
        {
            if (resetIndex || _chapterEntries.Count == 0)
                _chapterFocusIndex = 0;
            else
                _chapterFocusIndex = Math.Clamp(_chapterFocusIndex, 0, _chapterEntries.Count - 1);

            if (_chapterEntries.Count > 0)
            {
                GumFocusableButtonList.ClearFocus(_availableEntries);
                GumFocusableButtonList.ClearFocus(_footerEntries);
                GumFocusableButtonList.ApplyFocus(_chapterEntries, ref _chapterFocusIndex);
                EnsureColumnRowVisible();
            }
            else if (_availableEntries.Count > 0 && resetIndex)
            {
                // Prefer staying on Chapters empty; focus footer if neither useful.
            }
        }
        else
        {
            if (resetIndex)
                _footerFocusIndex = 0;
            else
                _footerFocusIndex = Math.Clamp(_footerFocusIndex, 0, Math.Max(0, _footerEntries.Count - 1));

            GumFocusableButtonList.ClearFocus(_availableEntries);
            GumFocusableButtonList.ClearFocus(_chapterEntries);
            if (_footerEntries.Count > 0)
                GumFocusableButtonList.ApplyFocus(_footerEntries, ref _footerFocusIndex);
        }
    }

    private void EnsureColumnRowVisible()
    {
        if (_focusZone == FocusZone.Available
            && _availableScroll is not null
            && _availableHost is not null
            && _availableEntries.Count > 0)
        {
            EditorFormScrollFocus.AfterNavigate(
                _availableScroll,
                _availableHost,
                _availableEntries,
                listFocusStartIndex: 0,
                listFocusCount: _availableEntries.Count,
                _availableFocusIndex,
                stackSpacing: 3f,
                minViewportHeight: 120f);
        }
        else if (_focusZone == FocusZone.Chapters
            && _chaptersScroll is not null
            && _chaptersHost is not null
            && _chapterEntries.Count > 0)
        {
            EditorFormScrollFocus.AfterNavigate(
                _chaptersScroll,
                _chaptersHost,
                _chapterEntries,
                listFocusStartIndex: 0,
                listFocusCount: _chapterEntries.Count,
                _chapterFocusIndex,
                stackSpacing: 3f,
                minViewportHeight: 120f);
        }
    }

    private IReadOnlyList<(Button Button, Action Activate)> CurrentColumnEntries() =>
        _focusZone == FocusZone.Chapters ? _chapterEntries : _availableEntries;

    private ref int CurrentColumnFocusIndex()
    {
        if (_focusZone == FocusZone.Chapters)
            return ref _chapterFocusIndex;
        return ref _availableFocusIndex;
    }

    private void RebuildLevelLists()
    {
        if (_availableHost is null || _chaptersHost is null)
            return;

        _availableEntries.Clear();
        _chapterEntries.Clear();
        _availableHost.Visual.Children.Clear();
        _chaptersHost.Visual.Children.Clear();

        if (_availableLevels.Count == 0)
        {
            var empty = new Label { Text = "(No levels — create levels first.)" };
            GumUiLayout.FillParentWidth(empty);
            _availableHost.AddChild(empty);
        }
        else
        {
            foreach (var levelId in _availableLevels)
            {
                var captured = levelId;
                var suffix = _chapters.Contains(captured, StringComparer.Ordinal) ? " [in]" : string.Empty;
                AddColumnButton(_availableHost, _availableEntries, captured + suffix, () => ShowAvailableLevelActions(captured));
            }
        }

        if (_chapters.Count == 0)
        {
            var empty = new Label { Text = "(Empty — add from the left.)" };
            GumUiLayout.FillParentWidth(empty);
            _chaptersHost.AddChild(empty);
        }
        else
        {
            for (var index = 0; index < _chapters.Count; index++)
            {
                var capturedIndex = index;
                var levelId = _chapters[index];
                AddColumnButton(
                    _chaptersHost,
                    _chapterEntries,
                    $"{index + 1}. {levelId}",
                    () => ShowChapterActions(capturedIndex));
            }
        }

        _availableFocusIndex = Math.Clamp(_availableFocusIndex, 0, Math.Max(0, _availableEntries.Count - 1));
        _chapterFocusIndex = Math.Clamp(_chapterFocusIndex, 0, Math.Max(0, _chapterEntries.Count - 1));

        if (_statusLabel is not null)
            _statusLabel.Text = StatusCaption();

        // Re-apply focus to the current zone after rebuild.
        SetFocusZone(_focusZone, resetIndex: false);
    }

    private void ShowAvailableLevelActions(string levelId)
    {
        if (_rootPanel is null)
            return;

        _choiceOverlay.Open(
            _rootPanel,
            "Level: " + levelId,
            [
                ("Add", () =>
                {
                    if (_chapters.Contains(levelId, StringComparer.Ordinal))
                    {
                        SyncStatus("Already in campaign: " + levelId);
                        return;
                    }

                    _chapters.Add(levelId);
                    SyncStatus("Added " + levelId + " as chapter " + _chapters.Count + ".");
                    RebuildLevelLists();
                }),
            ],
            onClosed: RestoreFocusAfterOverlay);
    }

    private void ShowChapterActions(int chapterIndex)
    {
        if (_rootPanel is null || chapterIndex < 0 || chapterIndex >= _chapters.Count)
            return;

        var levelId = _chapters[chapterIndex];
        _choiceOverlay.Open(
            _rootPanel,
            $"Chapter {chapterIndex + 1}: {levelId}",
            [
                ("Move Up", () =>
                {
                    if (chapterIndex <= 0)
                        return;
                    (_chapters[chapterIndex - 1], _chapters[chapterIndex]) =
                        (_chapters[chapterIndex], _chapters[chapterIndex - 1]);
                    RebuildLevelLists();
                }),
                ("Move Down", () =>
                {
                    if (chapterIndex >= _chapters.Count - 1)
                        return;
                    (_chapters[chapterIndex + 1], _chapters[chapterIndex]) =
                        (_chapters[chapterIndex], _chapters[chapterIndex + 1]);
                    RebuildLevelLists();
                }),
                ("Remove", () =>
                {
                    _chapters.RemoveAt(chapterIndex);
                    SyncStatus("Removed " + levelId + " from campaign.");
                    RebuildLevelLists();
                }),
            ],
            onClosed: RestoreFocusAfterOverlay);
    }

    private void RestoreFocusAfterOverlay() => SetFocusZone(_focusZone, resetIndex: false);

    private static Panel CreateColumn(
        Panel columns,
        string headerText,
        float listHeight,
        out Panel host,
        out ScrollViewer scroll)
    {
        var column = new Panel();
        column.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        column.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        column.Visual.StackSpacing = 4f;
        columns.AddChild(column);

        var header = new Label { Text = headerText };
        GumUiLayout.FillParentWidth(header);
        column.AddChild(header);

        scroll = new ScrollViewer();
        GumUiLayout.FillParentWidth(scroll);
        scroll.Visual.Height = listHeight;
        scroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        scroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        scroll.InnerPanel.Width = 0;
        scroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        GumScrollViewerChrome.DisableScrollChromeFocus(scroll);
        column.AddChild(scroll);

        host = new Panel();
        host.Visual.HasEvents = false;
        host.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        host.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        host.Visual.StackSpacing = 3f;
        GumUiLayout.FillParentWidth(host);
        scroll.AddChild(host);
        return column;
    }

    private string StatusCaption() =>
        _chapters.Count == 0
            ? "Add at least one chapter before Save."
            : _chapters.Count + " chapter(s) in order.";

    private static void AddTextField(Panel parent, string caption, string initialText, out TextBox textBox)
    {
        var label = new Label { Text = caption };
        GumUiLayout.FillParentWidth(label);
        parent.AddChild(label);
        textBox = new TextBox { Text = initialText };
        GumUiLayout.FillParentWidth(textBox);
        GumUiLayout.SetAbsoluteHeight(textBox, TextFieldHeight);
        EditorTextFieldStyle.Apply(textBox);
        parent.AddChild(textBox);
    }

    private static void AddColumnButton(
        Panel parent,
        List<(Button Button, Action Activate)> entries,
        string text,
        Action onClick)
    {
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) => onClick();
        parent.AddChild(button);
        entries.Add((button, onClick));
    }
}
