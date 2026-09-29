using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>New scenario module: Id/Title fields (same pattern as New Map).</summary>
public sealed class EditorNewScenarioView
{
    private const float TextFieldHeight = 40f;

    private Panel? _rootPanel;
    private Label? _statusLabel;
    private TextBox? _idBox;
    private TextBox? _titleBox;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;

    public string ModuleId =>
        string.IsNullOrWhiteSpace(_idBox?.Text) ? "user_scenario" : _idBox!.Text.Trim();

    public string ModuleTitle =>
        string.IsNullOrWhiteSpace(_titleBox?.Text) ? ModuleId : _titleBox!.Text.Trim();

    /// <summary>True while a TextBox owns keyboard focus (Backspace must edit text, not Cancel).</summary>
    public bool IsTextEntryActive =>
        _idBox is { IsFocused: true } || _titleBox is { IsFocused: true };

    public void Build(string defaultModuleId, string defaultTitle, Action onCreate, Action onBack)
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
        shell.Visual.Y = 16f;
        GumUiLayout.SetBoundedWidth(shell, maxPixels: 560f, parentPercent: 94f);
        shell.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        body.AddChild(shell);
        GumUiLayout.AddSolidBackground(shell, EditorUiColors.Panel);

        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: 8f, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        shell.AddChild(stack);

        GumUiLayout.AddVerticalSpacer(stack, 12f);

        var title = new Label { Text = "New Scenario Module" };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        var hint = new Label { Text = "Set Id/Title. Click a field to type." };
        GumUiLayout.FillParentWidth(hint);
        stack.AddChild(hint);

        AddTextField(stack, "Id (folder name)", defaultModuleId, out _idBox);
        AddTextField(stack, "Title (display name)", defaultTitle, out _titleBox);

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        stack.AddChild(_statusLabel);

        AddPlainButton(stack, "Create", onCreate);
        AddPlainButton(stack, "Back", onBack);

        GumUiLayout.AddVerticalSpacer(stack, 14f);
        FocusFirst();
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

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _focusableEntries,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds);
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _statusLabel = null;
        _idBox = null;
        _titleBox = null;
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;
    }

    private void FocusFirst()
    {
        _focusIndex = 0;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
    }

    private static void AddTextField(Panel parent, string caption, string initialText, out TextBox textBox)
    {
        var label = new Label { Text = caption };
        GumUiLayout.FillParentWidth(label);
        parent.AddChild(label);

        textBox = new TextBox { Text = initialText };
        GumUiLayout.FillParentWidth(textBox);
        GumUiLayout.SetAbsoluteHeight(textBox, TextFieldHeight);
        parent.AddChild(textBox);
    }

    private void AddPlainButton(Panel parent, string text, Action onClick)
    {
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) => onClick();
        parent.AddChild(button);
        _focusableEntries.Add((button, onClick));
    }
}
