using Gum;
using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>
/// Edit linear campaign chapters. Left column: id, title and available levels.
/// Right column: chapter order, then Save and Back.
/// </summary>
public sealed class EditorCampaignEditView
{
    private readonly EditorTwoColumnFormController _form = new(allowDpadColumnSwitch: true);
    private readonly EditorChoiceOverlay _choiceOverlay = new();
    private readonly List<string> _chapters = [];

    private Panel? _rootPanel;
    private Panel? _settingsHost;
    private Panel? _menuHost;
    private Label? _statusLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private IReadOnlyList<string> _availableLevels = [];
    private Action? _onSave;
    private Action? _onBack;
    private string _initialCampaignId = "campaign";
    private string _initialTitle = "campaign";
    private string _statusText = string.Empty;

    public bool IsTextEntryActive => EditorGumTextEntry.IsAnyFocused(_idBox, _titleBox);

    public bool IsChoiceOpen => _choiceOverlay.IsOpen;

    public string CampaignId =>
        _idBox is null
            ? _initialCampaignId
            : string.IsNullOrWhiteSpace(_idBox.Text) ? "campaign" : _idBox.Text;

    public string CampaignTitle =>
        string.IsNullOrWhiteSpace(_titleBox?.Text) ? CampaignId : _titleBox.Text;

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
        _initialCampaignId = string.IsNullOrWhiteSpace(campaignId) ? "campaign" : campaignId;
        _initialTitle = string.IsNullOrWhiteSpace(title) ? _initialCampaignId : title;
        _chapters.Clear();
        _chapters.AddRange(chapters.Where(id => !string.IsNullOrWhiteSpace(id)));
        _availableLevels = availableLevels ?? [];

        var built = EditorTwoColumnFormShell.Build(
            "Edit Campaign",
            "Left/Right or LB/RB switches columns. Up/Down moves the row. Confirm on a level opens actions. Save and Back are at the bottom of Chapters.",
            settingsWidthPercent: 48f,
            menuWidthPercent: 48f,
            settingsListMinHeight: 220f,
            settingsHeader: "Campaign",
            menuHeader: "Chapters",
            scrollMenuColumn: true);
        _rootPanel = built.RootPanel;
        _settingsHost = built.SettingsHost;
        _menuHost = built.MenuHost;
        _form.Attach(built);

        RebuildLists();
    }

    public void SyncStatus(string text)
    {
        _statusText = text ?? string.Empty;
        if (_statusLabel is not null)
            _statusLabel.Text = _statusText;
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

        _form.HandleInput(commands, elapsedSeconds);
    }

    public bool TryCloseChoice() => _choiceOverlay.TryClose();

    public void Clear()
    {
        _choiceOverlay.Close(notifyClosed: false);
        GumService.Default.Root.Children.Clear();
        _form.Clear();
        _rootPanel = null;
        _settingsHost = null;
        _menuHost = null;
        _statusLabel = null;
        _idBox = null;
        _titleBox = null;
        _availableLevels = [];
        _onSave = null;
        _onBack = null;
        _chapters.Clear();
        _statusText = string.Empty;
    }

    private void RebuildLists()
    {
        var campaignId = CampaignId;
        var title = CampaignTitle;
        _form.RebuildSettings(() => AddSettingsRows(campaignId, title));
        _form.RebuildMenu(AddMenuRows);
        if (_availableLevels.Count == 0 && !_form.IsMenuFocused)
            _form.FocusMenu(resetIndex: true);

        SyncStatus(StatusCaption());
    }

    private void AddSettingsRows(string campaignId, string title)
    {
        var settingsHost = _settingsHost;
        if (settingsHost is null)
            return;

        _statusLabel = new Label { Text = _statusText };
        GumUiLayout.FillParentWidth(_statusLabel);
        settingsHost.AddChild(_statusLabel);

        EditorTwoColumnFormShell.AddTextField(settingsHost, "Campaign Id", campaignId, out _idBox);
        EditorTwoColumnFormShell.AddTextField(settingsHost, "Title", title, out _titleBox);

        var levelsHeader = new Label { Text = "Available levels" };
        GumUiLayout.FillParentWidth(levelsHeader);
        settingsHost.AddChild(levelsHeader);

        if (_availableLevels.Count == 0)
        {
            AddNote(settingsHost, "(No levels — create levels first.)");
            return;
        }

        foreach (var levelId in _availableLevels)
        {
            var captured = levelId;
            var suffix = _chapters.Contains(captured, StringComparer.Ordinal) ? " [in]" : string.Empty;
            _form.AddSettingsButton(captured + suffix, () => ShowAvailableLevelActions(captured));
        }
    }

    private void AddMenuRows()
    {
        var menuHost = _menuHost;
        if (menuHost is null)
            return;

        if (_chapters.Count == 0)
            AddNote(menuHost, "(Empty — add from the left.)");

        for (var index = 0; index < _chapters.Count; index++)
        {
            var capturedIndex = index;
            var levelId = _chapters[index];
            _form.AddMenuButton(menuHost, $"{index + 1}. {levelId}", () => ShowChapterActions(capturedIndex));
        }

        _form.AddMenuButton(menuHost, "Save", () => _onSave?.Invoke());
        _form.AddMenuButton(menuHost, "Back", () => _onBack?.Invoke());
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
                        _form.RestoreFocus();
                        return;
                    }

                    _chapters.Add(levelId);
                    RebuildLists();
                    SyncStatus("Added " + levelId + " as chapter " + _chapters.Count + ".");
                }),
            ],
            onClosed: () => _form.RestoreFocus());
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
                    {
                        _form.RestoreFocus();
                        return;
                    }

                    (_chapters[chapterIndex - 1], _chapters[chapterIndex]) =
                        (_chapters[chapterIndex], _chapters[chapterIndex - 1]);
                    RebuildLists();
                }),
                ("Move Down", () =>
                {
                    if (chapterIndex >= _chapters.Count - 1)
                    {
                        _form.RestoreFocus();
                        return;
                    }

                    (_chapters[chapterIndex + 1], _chapters[chapterIndex]) =
                        (_chapters[chapterIndex], _chapters[chapterIndex + 1]);
                    RebuildLists();
                }),
                ("Remove", () =>
                {
                    _chapters.RemoveAt(chapterIndex);
                    RebuildLists();
                    SyncStatus("Removed " + levelId + " from campaign.");
                }),
            ],
            onClosed: () => _form.RestoreFocus());
    }

    private string StatusCaption() =>
        _chapters.Count == 0
            ? "Add at least one chapter before Save."
            : _chapters.Count + " chapter(s) in order.";

    private static void AddNote(Panel parent, string text)
    {
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        parent.AddChild(label);
    }
}
