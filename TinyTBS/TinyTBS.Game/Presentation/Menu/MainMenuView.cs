using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Menu;

/// <summary>Presentation: Gum tree for the main menu (GDD shell; unimplemented items greyed).</summary>
public sealed class MainMenuView
{
    private Panel? _rootPanel;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private int _focusIndex;

    public void Build(
        MainMenuViewModel viewModel,
        Action onContinue,
        Action onNewGame,
        Action onLoadGame,
        Action onContent,
        Action onEditor,
        Action onSettings,
        Action onAbout,
        Action onExit)
    {
        Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var bodyPanel = new Panel();
        bodyPanel.Dock(Dock.Fill);
        _rootPanel.AddChild(bodyPanel);

        var contentPanel = GumUiLayout.CreateVerticalStackPanel(spacing: 10f, widthPercent: 90f);
        GumUiLayout.SetBoundedWidth(contentPanel, maxPixels: 420f, parentPercent: 90f);
        GumUiLayout.CenterInParent(contentPanel, xPercent: 50f, yPercent: 42f);
        bodyPanel.AddChild(contentPanel);

        var title = new Label { Text = viewModel.Title };
        GumUiLayout.FillParentWidth(title);
        contentPanel.AddChild(title);

        AddMenuButton(contentPanel, "Continue", viewModel.CanContinue, onContinue);
        AddMenuButton(contentPanel, "New Game", viewModel.CanStartNewGame, onNewGame);
        AddMenuButton(contentPanel, "Load Game", viewModel.CanLoadGame, onLoadGame);
        AddMenuButton(contentPanel, "Content", viewModel.CanOpenContent, onContent);
        AddMenuButton(contentPanel, "Editor", viewModel.CanOpenEditor, onEditor);
        AddMenuButton(contentPanel, "Settings", viewModel.CanOpenSettings, onSettings);
        AddMenuButton(contentPanel, "About", viewModel.CanOpenAbout, onAbout);

        var exitButton = new Button { Text = "Exit" };
        GumUiLayout.PinToBottomRight(exitButton, insetPixels: 24f, widthPixels: 120f);
        exitButton.Click += (_, _) => onExit();
        _rootPanel.AddChild(exitButton);
        _focusableEntries.Add((exitButton, onExit));

        FocusFirst();
    }

    /// <summary>
    /// After <see cref="GumService"/> Update: D-pad / stick / arrows focus; Confirm activates.
    /// Mouse uses Gum clicks. Greyed items are not in the focus list.
    /// </summary>
    public void HandleInput(IGameCommandSource commands)
    {
        if (_focusableEntries.Count == 0)
            return;

        if (commands.WasPressed(GameCommand.NavigateDown))
        {
            _focusIndex = Math.Min(_focusIndex + 1, _focusableEntries.Count - 1);
            ApplyFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateUp))
        {
            _focusIndex = Math.Max(_focusIndex - 1, 0);
            ApplyFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.Confirm))
        {
            _focusableEntries[_focusIndex].Activate();
            return;
        }

        MaintainFocus();
    }

    public void FocusFirst()
    {
        _focusIndex = 0;
        ApplyFocusIndex();
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _focusableEntries.Clear();
        _focusIndex = 0;
    }

    private void AddMenuButton(Panel parent, string text, bool isEnabled, Action onClick)
    {
        var button = new Button { Text = text, IsEnabled = isEnabled };
        GumUiLayout.FillParentWidth(button);
        if (isEnabled)
        {
            button.Click += (_, _) => onClick();
            _focusableEntries.Add((button, onClick));
        }

        parent.AddChild(button);
    }

    private void MaintainFocus()
    {
        SyncFocusIndexFromUi();
        if (!IsOurFocusIntact())
            ApplyFocusIndex();
    }

    private bool IsOurFocusIntact()
    {
        if (_focusIndex < 0 || _focusIndex >= _focusableEntries.Count)
            return false;
        return _focusableEntries[_focusIndex].Button.IsFocused;
    }

    private void SyncFocusIndexFromUi()
    {
        for (var index = 0; index < _focusableEntries.Count; index++)
        {
            if (!_focusableEntries[index].Button.IsFocused)
                continue;
            _focusIndex = index;
            return;
        }
    }

    private void ApplyFocusIndex()
    {
        if (_focusableEntries.Count == 0)
            return;

        _focusIndex = Math.Clamp(_focusIndex, 0, _focusableEntries.Count - 1);
        foreach (var (button, _) in _focusableEntries)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }

        _focusableEntries[_focusIndex].Button.IsFocused = true;
    }
}
