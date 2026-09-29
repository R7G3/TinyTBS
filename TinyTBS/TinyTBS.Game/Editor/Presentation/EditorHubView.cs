using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Editor.ViewModels;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Editor hub: library list (left) + actions menu (right).</summary>
public sealed class EditorHubView
{
    private enum FocusZone
    {
        Library = 0,
        Menu = 1,
    }

    private const float StackSpacing = 6f;
    private const float ListMinHeight = 220f;
    private const float RowSpacing = 4f;

    private Panel? _rootPanel;
    private Panel? _shell;
    private Label? _statusLabel;
    private Label? _openLabel;
    private ScrollViewer? _libraryScroll;
    private Panel? _libraryHost;
    private readonly List<(Button Button, Action Activate)> _libraryEntries = [];
    private readonly List<(Button Button, Action Activate)> _menuEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private readonly EditorMapDetailOverlay _mapDetail = new();
    private readonly EditorLevelDetailOverlay _levelDetail = new();
    private FocusZone _focusZone = FocusZone.Library;
    private int _libraryFocusIndex;
    private int _menuFocusIndex;

    /// <summary>
    /// When true, ignore Gum Click and gamepad Confirm (used right after ReplaceScreen
    /// so a leftover click/Confirm cannot activate Hub Back → main menu).
    /// </summary>
    public bool SuppressActivations { get; set; }

    public bool IsMapDetailOpen => _mapDetail.IsOpen;

    public bool IsLevelDetailOpen => _levelDetail.IsOpen;

    public bool IsAnyDetailOpen => IsMapDetailOpen || IsLevelDetailOpen;

    public void Build(
        EditorHubViewModel viewModel,
        Action onNewScenario,
        Action onNewMap,
        Action onNewLevel,
        Action onEditCampaign,
        Action<EditorModuleRowViewModel> onActivateModule,
        Action<string> onOpenMap,
        Action<string> onDeleteMap,
        Action<string> onOpenLevel,
        Action<string> onDeleteLevel,
        Action onCloseModule,
        Action onBack)
    {
        Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var body = new Panel();
        body.Dock(Dock.Fill);
        _rootPanel.AddChild(body);

        _shell = new Panel();
        GumUiLayout.CenterHorizontallyInParent(_shell);
        _shell.Visual.Y = 12f;
        GumUiLayout.SetBoundedWidth(_shell, maxPixels: 780f, parentPercent: 96f);
        var canvasHeight = Math.Max(320f, GumService.Default.CanvasHeight);
        var shellHeight = Math.Max(320f, canvasHeight - 24f);
        const float topChrome = 10f + 26f + 22f + 22f + 22f + StackSpacing * 5f;
        const float bottomChrome = 10f;
        const float columnHeader = 26f + 4f;
        var listHeight = Math.Max(ListMinHeight, shellHeight - topChrome - bottomChrome - columnHeader);
        GumUiLayout.SetAbsoluteHeight(_shell, topChrome + bottomChrome + columnHeader + listHeight);
        body.AddChild(_shell);
        GumUiLayout.AddSolidBackground(_shell, EditorUiColors.Panel);

        var rootStack = GumUiLayout.CreateVerticalStackPanel(spacing: StackSpacing, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(rootStack);
        _shell.AddChild(rootStack);
        GumUiLayout.AddVerticalSpacer(rootStack, 10f);

        var title = new Label { Text = viewModel.Title };
        GumUiLayout.FillParentWidth(title);
        rootStack.AddChild(title);

        _statusLabel = new Label { Text = viewModel.StatusText };
        GumUiLayout.FillParentWidth(_statusLabel);
        rootStack.AddChild(_statusLabel);

        _openLabel = new Label
        {
            Text = viewModel.HasOpenModule
                ? $"Open: {viewModel.OpenModuleId} — {viewModel.OpenModuleTitle}"
                : "Open: (none)",
        };
        GumUiLayout.FillParentWidth(_openLabel);
        rootStack.AddChild(_openLabel);

        var hint = new Label
        {
            Text = "Left/Right (or LB/RB): library ↔ menu. Confirm opens detail. Back closes module, then leaves editor.",
        };
        GumUiLayout.FillParentWidth(hint);
        rootStack.AddChild(hint);

        var columns = new Panel();
        GumUiLayout.FillParentWidth(columns);
        columns.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        columns.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        columns.Visual.StackSpacing = 10f;
        rootStack.AddChild(columns);

        var leftColumn = CreateLibraryColumn(columns, listHeight);
        var rightColumn = CreateMenuColumn(
            columns,
            listHeight,
            viewModel,
            onNewScenario,
            onNewMap,
            onNewLevel,
            onEditCampaign,
            onCloseModule,
            onBack);
        GumUiLayout.SetWidthPercent(leftColumn, 58f);
        GumUiLayout.SetWidthPercent(rightColumn, 40f);

        PopulateLibrary(viewModel, onActivateModule, onOpenMap, onDeleteMap, onOpenLevel, onDeleteLevel);
        GumUiLayout.AddVerticalSpacer(rootStack, 10f);

        var startZone = _libraryEntries.Count > 0 ? FocusZone.Library : FocusZone.Menu;
        SetFocusZone(startZone, resetIndex: true);
    }

    public void SyncStatus(EditorHubViewModel viewModel)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = viewModel.StatusText ?? string.Empty;

        if (_openLabel is not null)
        {
            _openLabel.Text = viewModel.HasOpenModule
                ? $"Open: {viewModel.OpenModuleId} — {viewModel.OpenModuleTitle}"
                : "Open: (none)";
        }
    }

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (SuppressActivations)
            return;

        if (_mapDetail.IsOpen)
        {
            _mapDetail.HandleInput(commands, elapsedSeconds);
            return;
        }

        if (_levelDetail.IsOpen)
        {
            _levelDetail.HandleInput(commands, elapsedSeconds);
            return;
        }

        if (TrySwitchColumn(commands))
            return;

        if (_focusZone == FocusZone.Menu)
        {
            GumFocusableButtonList.HandleVerticalInput(
                commands,
                _menuEntries,
                ref _menuFocusIndex,
                _navigateRepeat,
                elapsedSeconds);
            return;
        }

        if (_libraryEntries.Count == 0)
            return;

        var result = GumFocusableButtonList.HandleVerticalInput(
            commands,
            _libraryEntries,
            ref _libraryFocusIndex,
            _navigateRepeat,
            elapsedSeconds);

        if (result == GumFocusListResult.Navigated)
            EnsureLibraryRowVisible();
        else
            GumScrollViewerChrome.StealFocusFromScrollChrome(_libraryScroll);
    }

    public bool TryCloseMapDetail() => _mapDetail.TryClose();

    public bool TryCloseLevelDetail() => _levelDetail.TryClose();

    public bool TryCloseAnyDetail() => TryCloseMapDetail() || TryCloseLevelDetail();

    public void ApplyResponsiveLayout()
    {
        // Absolute shell sized in Build; nothing to reflow on resize until rebuild.
    }

    public void FocusFirst()
    {
        if (IsAnyDetailOpen)
            return;
        SetFocusZone(_libraryEntries.Count > 0 ? FocusZone.Library : FocusZone.Menu, resetIndex: true);
    }

    public void Clear()
    {
        _mapDetail.Close(notifyClosed: false);
        _levelDetail.Close(notifyClosed: false);
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _shell = null;
        _statusLabel = null;
        _openLabel = null;
        _libraryScroll = null;
        _libraryHost = null;
        _libraryEntries.Clear();
        _menuEntries.Clear();
        _navigateRepeat.Reset();
        _focusZone = FocusZone.Library;
        _libraryFocusIndex = 0;
        _menuFocusIndex = 0;
    }

    private void PopulateLibrary(
        EditorHubViewModel viewModel,
        Action<EditorModuleRowViewModel> onActivateModule,
        Action<string> onOpenMap,
        Action<string> onDeleteMap,
        Action<string> onOpenLevel,
        Action<string> onDeleteLevel)
    {
        if (_libraryHost is null)
            return;

        if (viewModel.HasOpenModule && viewModel.Maps.Count > 0)
        {
            AddLibraryHeader("Maps");
            foreach (var mapId in viewModel.Maps)
            {
                var captured = mapId;
                AddLibraryButton("Map: " + captured, () => ShowMapDetail(captured, onOpenMap, onDeleteMap));
            }
        }

        if (viewModel.HasOpenModule && viewModel.Levels.Count > 0)
        {
            AddLibraryHeader("Levels");
            foreach (var levelId in viewModel.Levels)
            {
                var captured = levelId;
                AddLibraryButton("Level: " + captured, () => ShowLevelDetail(captured, onOpenLevel, onDeleteLevel));
            }
        }

        if (viewModel.Modules.Count == 0)
        {
            var empty = new Label { Text = "(No modules — create a scenario or duplicate bundled.)" };
            GumUiLayout.FillParentWidth(empty);
            _libraryHost.AddChild(empty);
        }
        else
        {
            AddLibraryHeader("Modules");
            foreach (var module in viewModel.Modules)
            {
                var captured = module;
                AddLibraryButton(captured.SummaryLine, () => onActivateModule(captured));
            }
        }
    }

    private Panel CreateLibraryColumn(Panel columns, float listHeight)
    {
        var column = new Panel();
        column.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        column.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        column.Visual.StackSpacing = 4f;
        columns.AddChild(column);

        var header = new Label { Text = "Library" };
        GumUiLayout.FillParentWidth(header);
        column.AddChild(header);

        _libraryScroll = new ScrollViewer();
        GumUiLayout.FillParentWidth(_libraryScroll);
        _libraryScroll.Visual.Height = listHeight;
        _libraryScroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        _libraryScroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        _libraryScroll.InnerPanel.Width = 0;
        _libraryScroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        _libraryScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ApplyListWellBackground(_libraryScroll);
        GumScrollViewerChrome.DisableScrollChromeFocus(_libraryScroll);
        column.AddChild(_libraryScroll);

        _libraryHost = new Panel();
        _libraryHost.Visual.HasEvents = false;
        _libraryHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _libraryHost.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _libraryHost.Visual.StackSpacing = RowSpacing;
        GumUiLayout.FillParentWidth(_libraryHost);
        _libraryScroll.AddChild(_libraryHost);
        return column;
    }

    private Panel CreateMenuColumn(
        Panel columns,
        float listHeight,
        EditorHubViewModel viewModel,
        Action onNewScenario,
        Action onNewMap,
        Action onNewLevel,
        Action onEditCampaign,
        Action onCloseModule,
        Action onBack)
    {
        var column = new Panel();
        column.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        column.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        column.Visual.StackSpacing = 4f;
        columns.AddChild(column);

        var header = new Label { Text = "Menu" };
        GumUiLayout.FillParentWidth(header);
        column.AddChild(header);

        var host = new Panel();
        GumUiLayout.FillParentWidth(host);
        host.Visual.Height = listHeight;
        host.Visual.HeightUnits = DimensionUnitType.Absolute;
        host.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        host.Visual.StackSpacing = 6f;
        column.AddChild(host);

        AddMenuButton(host, "New Scenario Module", onNewScenario);
        AddMenuButton(host, "New Map", onNewMap, isEnabled: viewModel.CanCreateMap);
        AddMenuButton(host, "New Level", onNewLevel, isEnabled: viewModel.CanEditScenarioContent);
        AddMenuButton(host, "Edit Campaign", onEditCampaign, isEnabled: viewModel.CanEditScenarioContent);
        AddMenuButton(host, "Close Module", onCloseModule, isEnabled: viewModel.HasOpenModule);
        AddMenuButton(host, "Publish", () => { }, isEnabled: viewModel.CanPublish);
        AddMenuButton(host, "Back", onBack);
        return column;
    }

    private bool TrySwitchColumn(IGameCommandSource commands)
    {
        // No horizontal button rows in either column — Left/Right switch columns (plus LB/RB).
        var toMenu = commands.WasPressed(GameCommand.NavigateRight)
            || commands.WasPressed(GameCommand.FocusNextRegion)
            || commands.WasPressed(GameCommand.ZoomIn);
        var toLibrary = commands.WasPressed(GameCommand.NavigateLeft)
            || commands.WasPressed(GameCommand.FocusPreviousRegion)
            || commands.WasPressed(GameCommand.ZoomOut);

        if (toMenu && _focusZone == FocusZone.Library)
        {
            SetFocusZone(FocusZone.Menu, resetIndex: false);
            return true;
        }

        if (toLibrary && _focusZone == FocusZone.Menu)
        {
            if (_libraryEntries.Count == 0)
                return true;
            SetFocusZone(FocusZone.Library, resetIndex: false);
            return true;
        }

        return (toMenu && _focusZone == FocusZone.Menu) || (toLibrary && _focusZone == FocusZone.Library);
    }

    private void SetFocusZone(FocusZone zone, bool resetIndex)
    {
        if (zone == FocusZone.Library && _libraryEntries.Count == 0)
            zone = FocusZone.Menu;

        _focusZone = zone;
        if (zone == FocusZone.Library)
        {
            if (resetIndex)
                _libraryFocusIndex = 0;
            else
                _libraryFocusIndex = Math.Clamp(_libraryFocusIndex, 0, _libraryEntries.Count - 1);

            GumFocusableButtonList.ClearFocus(_menuEntries);
            GumFocusableButtonList.ApplyFocus(_libraryEntries, ref _libraryFocusIndex);
            EnsureLibraryRowVisible();
        }
        else
        {
            if (resetIndex)
                _menuFocusIndex = 0;
            else
                _menuFocusIndex = Math.Clamp(_menuFocusIndex, 0, Math.Max(0, _menuEntries.Count - 1));

            GumFocusableButtonList.ClearFocus(_libraryEntries);
            if (_menuEntries.Count > 0)
                GumFocusableButtonList.ApplyFocus(_menuEntries, ref _menuFocusIndex);
        }
    }

    private void EnsureLibraryRowVisible()
    {
        if (_libraryEntries.Count == 0 || _libraryScroll is null || _libraryHost is null)
            return;

        EditorFormScrollFocus.AfterNavigate(
            _libraryScroll,
            _libraryHost,
            _libraryEntries,
            listFocusStartIndex: 0,
            listFocusCount: _libraryEntries.Count,
            _libraryFocusIndex,
            RowSpacing,
            minViewportHeight: 120f);
    }

    private void ShowMapDetail(string mapId, Action<string> onOpenMap, Action<string> onDeleteMap)
    {
        if (_rootPanel is null)
            return;

        _mapDetail.Open(
            _rootPanel,
            mapId,
            onOpen: () =>
            {
                _mapDetail.Close(notifyClosed: false);
                onOpenMap(mapId);
            },
            onDelete: () =>
            {
                _mapDetail.Close(notifyClosed: false);
                onDeleteMap(mapId);
            },
            onClosed: () => SetFocusZone(FocusZone.Library, resetIndex: false));
    }

    private void ShowLevelDetail(string levelId, Action<string> onOpenLevel, Action<string> onDeleteLevel)
    {
        if (_rootPanel is null)
            return;

        _levelDetail.Open(
            _rootPanel,
            levelId,
            onOpen: () =>
            {
                _levelDetail.Close(notifyClosed: false);
                onOpenLevel(levelId);
            },
            onDelete: () =>
            {
                _levelDetail.Close(notifyClosed: false);
                onDeleteLevel(levelId);
            },
            onClosed: () => SetFocusZone(FocusZone.Library, resetIndex: false));
    }

    private void AddLibraryHeader(string text)
    {
        if (_libraryHost is null)
            return;
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        _libraryHost.AddChild(label);
    }

    private void AddLibraryButton(string text, Action onClick)
    {
        if (_libraryHost is null)
            return;

        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) =>
        {
            for (var i = 0; i < _libraryEntries.Count; i++)
            {
                if (!ReferenceEquals(_libraryEntries[i].Button, button))
                    continue;
                _libraryFocusIndex = i;
                break;
            }

            SetFocusZone(FocusZone.Library, resetIndex: false);
            if (!SuppressActivations)
                onClick();
        };
        _libraryHost.AddChild(button);
        _libraryEntries.Add((button, () =>
        {
            if (!SuppressActivations)
                onClick();
        }));
    }

    private void AddMenuButton(Panel parent, string text, Action onClick, bool isEnabled = true)
    {
        var button = new Button { Text = text, IsEnabled = isEnabled };
        GumUiLayout.FillParentWidth(button);
        if (isEnabled)
        {
            button.Click += (_, _) =>
            {
                for (var i = 0; i < _menuEntries.Count; i++)
                {
                    if (!ReferenceEquals(_menuEntries[i].Button, button))
                        continue;
                    _menuFocusIndex = i;
                    break;
                }

                SetFocusZone(FocusZone.Menu, resetIndex: false);
                if (!SuppressActivations)
                    onClick();
            };
            parent.AddChild(button);
            _menuEntries.Add((button, () =>
            {
                if (!SuppressActivations)
                    onClick();
            }));
        }
        else
        {
            button.Visual.HasEvents = false;
            parent.AddChild(button);
        }
    }

    private static void ApplyListWellBackground(ScrollViewer scrollViewer)
    {
        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return;

        visual.BackgroundColor = EditorUiColors.ListWell;
    }
}
