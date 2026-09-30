using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Content;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Presentation.About;

/// <summary>About panel: credits, clickable links, library list, scroll + gamepad.</summary>
public sealed class AboutView
{
    private const float PanelMaxWidth = 640f;
    private const float ScrollMinHeight = 180f;
    private const float ScrollStepPixels = 36f;
    private const float StickScrollDeadzone = 0.35f;
    private const float StackSpacing = 8f;

    public const string SiteUrl = "https://github.com/R7G3/TinyTBS";
    public const string MonoGameUrl = "https://monogame.net/";

    private Panel? _rootPanel;
    private ScrollViewer? _scroll;
    private Panel? _scrollHost;
    private Label? _statusLabel;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private readonly HeldCommandRepeatState _scrollUpRepeat = new();
    private readonly HeldCommandRepeatState _scrollDownRepeat = new();
    private int _focusIndex;
    private float _stickScrollCooldown;

    public void Build(Action onBack, Action<string> onOpenUrl)
    {
        Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var body = new Panel();
        body.Dock(Dock.Fill);
        _rootPanel.AddChild(body);

        var shell = new Panel();
        GumUiLayout.CenterInParent(shell, xPercent: 50f, yPercent: 50f);
        GumUiLayout.SetBoundedWidth(shell, PanelMaxWidth, parentPercent: 92f);
        var canvasHeight = Math.Max(320f, GumService.Default.CanvasHeight);
        var shellHeight = Math.Max(300f, Math.Min(canvasHeight - 24f, 560f));
        GumUiLayout.SetAbsoluteHeight(shell, shellHeight);
        body.AddChild(shell);
        GumUiLayout.AddSolidBackground(shell, UiColors.MenuPanel);

        var rootStack = GumUiLayout.CreateVerticalStackPanel(spacing: StackSpacing, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(rootStack);
        shell.AddChild(rootStack);
        GumUiLayout.AddVerticalSpacer(rootStack, 12f);

        var title = new Label { Text = "About TinyTBS" };
        GumUiLayout.FillParentWidth(title);
        rootStack.AddChild(title);

        var hint = new Label
        {
            Text = "Up/Down or sticks: scroll. LB/RB: links ↔ Back. Confirm opens link / Back.",
        };
        GumUiLayout.FillParentWidth(hint);
        rootStack.AddChild(hint);

        _statusLabel = new Label { Text = string.Empty };
        GumUiLayout.FillParentWidth(_statusLabel);
        rootStack.AddChild(_statusLabel);

        const float topInset = 12f;
        const float bottomInset = 4f;
        const float chrome = topInset + 26f + 22f + 22f + 26f + StackSpacing * 6f + bottomInset;
        var scrollHeight = Math.Max(ScrollMinHeight, shellHeight - chrome);

        _scroll = new ScrollViewer();
        GumUiLayout.FillParentWidth(_scroll);
        _scroll.Visual.Height = scrollHeight;
        _scroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        _scroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        _scroll.InnerPanel.Width = 0;
        _scroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        _scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ApplyListWellBackground(_scroll);
        GumScrollViewerChrome.DisableScrollChromeFocus(_scroll);
        rootStack.AddChild(_scroll);

        _scrollHost = new Panel();
        _scrollHost.Visual.HasEvents = false;
        _scrollHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _scrollHost.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _scrollHost.Visual.StackSpacing = StackSpacing;
        GumUiLayout.FillParentWidth(_scrollHost);
        _scroll.AddChild(_scrollHost);

        AddBodyLine(_scrollHost, "Author: Vadim (R7G3) Trofimov");
        AddBodyLine(_scrollHost, "Site:");
        AddLinkButton(_scrollHost, SiteUrl, () => onOpenUrl(SiteUrl));
        AddBodyLine(_scrollHost, "Based on:");
        AddLinkButton(_scrollHost, MonoGameUrl, () => onOpenUrl(MonoGameUrl));
        AddBodyLine(_scrollHost, "Used libs:");
        foreach (var libraryLine in AboutSolutionLibraries.Lines)
            AddBodyLine(_scrollHost, "  • " + libraryLine);

        GumUiLayout.AddVerticalSpacer(_scrollHost, 16f);

        var backButton = new Button { Text = "Back" };
        GumUiLayout.FillParentWidth(backButton);
        backButton.Click += (_, _) =>
        {
            _focusIndex = _focusableEntries.Count - 1;
            onBack();
        };
        rootStack.AddChild(backButton);
        _focusableEntries.Add((backButton, onBack));

        GumUiLayout.AddVerticalSpacer(rootStack, bottomInset);
        FocusBack();
    }

    public void SyncStatus(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text ?? string.Empty;
    }

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (TryScroll(commands, elapsedSeconds))
        {
            GumScrollViewerChrome.StealFocusFromScrollChrome(_scroll);
            return;
        }

        if (TryCycleFocus(commands))
            return;

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _focusableEntries,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds,
            mapHorizontalToVertical: false);
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _scroll = null;
        _scrollHost = null;
        _statusLabel = null;
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _scrollUpRepeat.Reset();
        _scrollDownRepeat.Reset();
        _focusIndex = 0;
        _stickScrollCooldown = 0f;
    }

    private bool TryScroll(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_scroll is null)
            return false;

        var delta = 0;
        if (_scrollUpRepeat.TryTick(commands, GameCommand.NavigateUp, elapsedSeconds))
            delta = -1;
        else if (_scrollDownRepeat.TryTick(commands, GameCommand.NavigateDown, elapsedSeconds))
            delta = +1;

        var stickY = commands.CameraPanStick.Y;
        // Also treat strong left-stick vertical as scroll when already covered by Navigate*;
        // right stick (CameraPan) always scrolls here.
        if (delta == 0 && Math.Abs(stickY) >= StickScrollDeadzone)
        {
            _stickScrollCooldown -= elapsedSeconds;
            if (_stickScrollCooldown <= 0f)
            {
                delta = stickY > 0f ? -1 : +1;
                _stickScrollCooldown = HeldCommandRepeat.DefaultIntervalSeconds;
            }
        }
        else if (Math.Abs(stickY) < StickScrollDeadzone)
        {
            _stickScrollCooldown = 0f;
        }

        if (delta == 0)
            return false;

        var next = _scroll.VerticalScrollBarValue + (delta * ScrollStepPixels);
        _scroll.VerticalScrollBarValue = Math.Max(0f, next);
        return true;
    }

    private bool TryCycleFocus(IGameCommandSource commands)
    {
        if (_focusableEntries.Count == 0)
            return false;

        var toNext = commands.WasPressed(GameCommand.FocusNextRegion)
            || commands.WasPressed(GameCommand.ZoomIn)
            || commands.WasPressed(GameCommand.NavigateRight);
        var toPrev = commands.WasPressed(GameCommand.FocusPreviousRegion)
            || commands.WasPressed(GameCommand.ZoomOut)
            || commands.WasPressed(GameCommand.NavigateLeft);

        if (!toNext && !toPrev)
            return false;

        if (toNext)
            _focusIndex = (_focusIndex + 1) % _focusableEntries.Count;
        else
            _focusIndex = (_focusIndex - 1 + _focusableEntries.Count) % _focusableEntries.Count;

        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
        EnsureFocusedLinkVisible();
        return true;
    }

    private void EnsureFocusedLinkVisible()
    {
        if (_scroll is null
            || _scrollHost is null
            || _focusIndex < 0
            || _focusIndex >= _focusableEntries.Count - 1)
        {
            return;
        }

        // Last entry is Back (outside scroll).
        if (_focusableEntries[_focusIndex].Button.Visual is not { } focusedVisual)
            return;

        GumScrollViewerChrome.StealFocusFromScrollChrome(_scroll);
        GumScrollListLayout.EnsureFocusedRowVisible(
            _scroll,
            _scrollHost,
            focusedVisual,
            listFocusStartIndex: 0,
            listFocusCount: _focusableEntries.Count - 1,
            _focusIndex,
            StackSpacing,
            minViewportHeight: 80f);
    }

    private void FocusBack()
    {
        if (_focusableEntries.Count == 0)
            return;
        _focusIndex = _focusableEntries.Count - 1;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
    }

    private void AddLinkButton(Panel parent, string url, Action onClick)
    {
        var button = new Button { Text = url };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) =>
        {
            for (var i = 0; i < _focusableEntries.Count; i++)
            {
                if (!ReferenceEquals(_focusableEntries[i].Button, button))
                    continue;
                _focusIndex = i;
                break;
            }

            GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
            onClick();
        };
        parent.AddChild(button);
        _focusableEntries.Add((button, onClick));
    }

    private static void AddBodyLine(Panel parent, string text)
    {
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        parent.AddChild(label);
    }

    private static void ApplyListWellBackground(ScrollViewer scrollViewer)
    {
        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return;
        visual.BackgroundColor = UiColors.MenuListWell;
    }

    /// <summary>Per-command repeat timers for scroll (Up and Down independently).</summary>
    private sealed class HeldCommandRepeatState
    {
        private float _timer;

        public void Reset() => _timer = 0f;

        public bool TryTick(IGameCommandSource commands, GameCommand command, float elapsedSeconds) =>
            HeldCommandRepeat.TryTick(commands, command, elapsedSeconds, ref _timer);
    }
}
