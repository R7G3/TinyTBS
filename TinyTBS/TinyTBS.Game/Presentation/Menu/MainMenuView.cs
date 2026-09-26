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
    private readonly List<Button> _focusableButtons = [];
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
        _focusableButtons.Add(exitButton);

        FocusFirst();
    }

    /// <summary>
    /// Call after <see cref="GumService"/> Update. Owns D-pad / stick focus so greyed items are skipped.
    /// </summary>
    public void HandleGamepadNavigation(IGameCommandSource commands)
    {
        if (_focusableButtons.Count == 0)
            return;

        if (commands.WasPressed(GameCommand.NavigateDown))
        {
            _focusIndex = Math.Min(_focusIndex + 1, _focusableButtons.Count - 1);
            ApplyFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateUp))
        {
            _focusIndex = Math.Max(_focusIndex - 1, 0);
            ApplyFocusIndex();
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
        _focusableButtons.Clear();
        _focusIndex = 0;
    }

    private void AddMenuButton(Panel parent, string text, bool isEnabled, Action onClick)
    {
        var button = new Button { Text = text, IsEnabled = isEnabled };
        GumUiLayout.FillParentWidth(button);
        if (isEnabled)
        {
            button.Click += (_, _) => onClick();
            _focusableButtons.Add(button);
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
        if (_focusIndex < 0 || _focusIndex >= _focusableButtons.Count)
            return false;
        return _focusableButtons[_focusIndex].IsFocused;
    }

    private void SyncFocusIndexFromUi()
    {
        for (var index = 0; index < _focusableButtons.Count; index++)
        {
            if (!_focusableButtons[index].IsFocused)
                continue;
            _focusIndex = index;
            return;
        }
    }

    private void ApplyFocusIndex()
    {
        if (_focusableButtons.Count == 0)
            return;

        _focusIndex = Math.Clamp(_focusIndex, 0, _focusableButtons.Count - 1);
        ClearAllFocusFlags();
        _focusableButtons[_focusIndex].IsFocused = true;
    }

    private void ClearAllFocusFlags()
    {
        foreach (var button in _focusableButtons)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }
    }
}
