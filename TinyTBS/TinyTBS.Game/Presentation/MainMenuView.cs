using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation;

/// <summary>
/// Presentation: Gum tree for the main menu.
/// </summary>
public sealed class MainMenuView
{
    private Panel? _rootPanel;
    private ComboBox? _modComboBox;

    public void Build(
        MainMenuViewModel viewModel,
        Action onStartMatch,
        Action onExit,
        Action onModSelectionChanged)
    {
        Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var bodyPanel = new Panel();
        bodyPanel.Dock(Dock.Fill);
        _rootPanel.AddChild(bodyPanel);

        var contentPanel = GumUiLayout.CreateVerticalStackPanel(spacing: 14f, widthPercent: 90f);
        GumUiLayout.CenterInParent(contentPanel, xPercent: 50f, yPercent: 45f);
        bodyPanel.AddChild(contentPanel);

        var title = new Label { Text = viewModel.Title };
        GumUiLayout.FillParentWidth(title);
        contentPanel.AddChild(title);

        var modLabel = new Label { Text = "Graphics mod" };
        GumUiLayout.FillParentWidth(modLabel);
        contentPanel.AddChild(modLabel);

        _modComboBox = new ComboBox
        {
            Items = viewModel.ModOptions.ToList()
        };
        GumUiLayout.FillParentWidth(_modComboBox);

        var selectedIndex = viewModel.ModOptions
            .ToList()
            .FindIndex(option => option == viewModel.SelectedModOption);
        _modComboBox.SelectedIndex = Math.Max(selectedIndex, 0);
        _modComboBox.SelectionChanged += (_, _) => onModSelectionChanged();
        contentPanel.AddChild(_modComboBox);

        var hint = new Label
        {
            Text = "Vanilla uses bundled content. Mods load from Mods/ next to the game."
        };
        GumUiLayout.FillParentWidth(hint);
        contentPanel.AddChild(hint);

        var startButton = new Button { Text = "Start match" };
        GumUiLayout.FillParentWidth(startButton);
        startButton.Click += (_, _) => onStartMatch();
        contentPanel.AddChild(startButton);

        var exitButton = new Button { Text = "Exit" };
        GumUiLayout.PinToBottomRight(exitButton, insetPixels: 24f, widthPercent: 14f);
        exitButton.Click += (_, _) => onExit();
        _rootPanel.AddChild(exitButton);

        _modComboBox.IsFocused = true;
    }

    public bool TryGetSelectedMod(out string selected)
    {
        selected = string.Empty;
        if (_modComboBox is null)
            return false;

        selected = _modComboBox.SelectedObject as string
            ?? _modComboBox.Text
            ?? string.Empty;
        return !string.IsNullOrWhiteSpace(selected);
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _modComboBox = null;
    }
}
