using Gum;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Edit Maps/{id}/script.cs via Gum multiline TextBox.</summary>
public sealed class EditorMapScriptView
{
    private Panel? _rootPanel;
    private Label? _statusLabel;
    private TextBox? _scriptBox;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;

    public bool IsTextEntryActive => _scriptBox is { IsFocused: true };

    public string ScriptText => _scriptBox?.Text ?? string.Empty;

    public void Build(string mapId, string initialScript, Action onSave, Action onApplyTemplate, Action onBack)
    {
        Clear();

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

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 8f, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        shell.AddChild(stack);
        GumUiLayout.AddVerticalSpacer(stack, 10f);

        var title = new Label { Text = "Map Script — " + mapId };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        var hint = new Label
        {
            Text = "Multiline TextBox: Enter = new line. Click the field to type. Template resets hooks.",
        };
        GumUiLayout.FillParentWidth(hint);
        stack.AddChild(hint);

        // Fill space between header and action row (no nested ScrollViewer — TextBox scrolls itself).
        var reservedChrome = 10f + 28f + 28f + 8f + 28f + 8f + 48f + 14f;
        var scriptHeight = Math.Max(180f, canvasHeight * 0.94f - reservedChrome - 40f);

        _scriptBox = new TextBox
        {
            Text = NormalizeNewlines(initialScript),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
        };
        GumUiLayout.FillParentWidth(_scriptBox);
        GumUiLayout.SetAbsoluteHeight(_scriptBox, scriptHeight);
        EditorTextFieldStyle.Apply(_scriptBox);
        stack.AddChild(_scriptBox);

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        stack.AddChild(_statusLabel);

        var actionsHost = new Panel();
        GumUiLayout.FillParentWidth(actionsHost);
        actionsHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.AddChild(actionsHost);

        var templateButton = new Button { Text = "Apply Template" };
        templateButton.Click += (_, _) => onApplyTemplate();
        var saveButton = new Button { Text = "Save" };
        saveButton.Click += (_, _) => onSave();
        var backButton = new Button { Text = "Back" };
        backButton.Click += (_, _) => onBack();

        GumUiLayout.LayoutAdaptiveButtonRows(
            actionsHost,
            [templateButton, saveButton, backButton],
            availableWidth: 700f,
            spacing: 8f,
            minButtonWidth: 100f,
            preferredButtonWidth: 180f);

        _focusableEntries.Add((templateButton, onApplyTemplate));
        _focusableEntries.Add((saveButton, onSave));
        _focusableEntries.Add((backButton, onBack));
        GumUiLayout.AddVerticalSpacer(stack, 10f);
        FocusFirst();
    }

    public void SetScriptText(string text)
    {
        if (_scriptBox is null)
            return;
        _scriptBox.Text = NormalizeNewlines(text);
        _scriptBox.CaretIndex = 0;
    }

    public void SyncStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text ?? string.Empty;
    }

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (IsTextEntryActive)
            return;

        // Horizontal row of actions — Left/Right + Confirm.
        GumFocusableButtonList.HandleHorizontalInput(
            commands,
            _focusableEntries,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds);

        // Also allow Up/Down as Left/Right for pad users who stay on D-pad vertical habit.
        var vertical = _navigateRepeat.TryGetDelta(commands, elapsedSeconds);
        if (vertical != 0 && _focusableEntries.Count > 0)
        {
            _focusIndex = Math.Clamp(_focusIndex + vertical, 0, _focusableEntries.Count - 1);
            GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
        }
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _statusLabel = null;
        _scriptBox = null;
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;
    }

    private void FocusFirst()
    {
        _focusIndex = 0;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
    }

    private static string NormalizeNewlines(string text) =>
        (text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
}
