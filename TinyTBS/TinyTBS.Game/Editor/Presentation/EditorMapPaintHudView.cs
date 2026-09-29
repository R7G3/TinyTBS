using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Editor.Validation;
using TinyTBS.Game.Input;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>
/// Map paint HUD: left Content (tabs + icons), right Tools; LB/RB cycles Content/Map/Tools.
/// </summary>
public sealed class EditorMapPaintHudView
{
    private const float SidePanelWidth = 188f;
    private const float IconSize = 32f;
    private const float PanelTop = 40f;
    private const float PanelBottomInset = 12f;

    private Panel? _rootPanel;
    private Panel? _leftPanel;
    private Panel? _rightPanel;
    private Panel? _contentListHost;
    private Label? _statusLabel;
    private RectangleRuntime? _validationDot;
    private readonly List<(Button Button, Action Activate)> _contentEntries = [];
    private readonly List<(Button Button, Action Activate)> _toolEntries = [];
    private readonly List<(Button Button, Action Activate)> _tabEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _contentFocusIndex;
    private int _toolFocusIndex;
    private int _tabFocusIndex;
    private bool _tabsFocused;
    private EditorPaintFocusZone _focusZone = EditorPaintFocusZone.Map;
    private EditorContentTab _contentTab = EditorContentTab.Terrain;
    private EditorValidationIndicator _validationIndicator = EditorValidationIndicator.Idle;

    private EditorPaintToolState? _tool;
    private MatchTextureAtlas? _textures;
    private MatchContentCatalog? _catalog;
    private Action? _onToolChanged;

    public EditorPaintFocusZone FocusZone => _focusZone;

    public bool IsMapFocused => _focusZone == EditorPaintFocusZone.Map;

    public bool IsPointerOverUi
    {
        get
        {
            var over = GumService.Default.Cursor.FrameworkElementOver;
            if (over is null)
                return false;
            return IsOverPanel(over, _leftPanel) || IsOverPanel(over, _rightPanel);
        }
    }

    public void Build(
        string statusText,
        EditorPaintToolState tool,
        MatchTextureAtlas textures,
        MatchContentCatalog catalog,
        Action onSave,
        Action onScript,
        Action onBack,
        Action onUndo,
        Action onRedo,
        Action onValidate,
        Action onToolChanged)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(textures);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(onToolChanged);
        Clear();

        _tool = tool;
        _textures = textures;
        _catalog = catalog;
        _onToolChanged = onToolChanged;

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.Visual.HasEvents = false;
        _rootPanel.AddToRoot();

        var top = GumUiLayout.CreateVerticalStackPanel(spacing: 2f, widthPercent: 56f);
        GumUiLayout.CenterHorizontallyInParent(top);
        top.Visual.Y = 6f;
        top.Visual.HasEvents = false;
        _rootPanel.AddChild(top);

        var statusRow = new Panel();
        GumUiLayout.FillParentWidth(statusRow);
        statusRow.Visual.Height = 22f;
        statusRow.Visual.HeightUnits = DimensionUnitType.Absolute;
        statusRow.Visual.HasEvents = false;
        top.AddChild(statusRow);

        _validationDot = new RectangleRuntime
        {
            Width = 12f,
            Height = 12f,
            X = 0f,
            Y = 4f,
            IsFilled = true,
        };
        _validationDot.WidthUnits = DimensionUnitType.Absolute;
        _validationDot.HeightUnits = DimensionUnitType.Absolute;
        _validationDot.FillColor = IndicatorColor(_validationIndicator);
        statusRow.AddChild(_validationDot);

        _statusLabel = new Label { Text = statusText };
        _statusLabel.Visual.HasEvents = false;
        _statusLabel.X = 18f;
        GumUiLayout.FillParentWidth(_statusLabel);
        statusRow.AddChild(_statusLabel);

        var canvasHeight = Math.Max(320f, GumService.Default.CanvasHeight);
        var panelHeight = Math.Clamp(canvasHeight - PanelTop - PanelBottomInset, 180f, canvasHeight - PanelTop - 8f);

        _leftPanel = CreateSidePanel(Anchor.Left, panelHeight, out var leftStack);
        _leftPanel.X = 8f;
        _leftPanel.Y = PanelTop;
        _rootPanel.AddChild(_leftPanel);
        BuildContentChrome(leftStack, panelHeight);

        _rightPanel = CreateSidePanel(Anchor.Right, panelHeight, out var rightStack);
        _rightPanel.X = -8f;
        _rightPanel.Y = PanelTop;
        _rootPanel.AddChild(_rightPanel);
        FillToolsPanel(
            rightStack,
            panelHeight,
            tool,
            onSave,
            onScript,
            onBack,
            onUndo,
            onRedo,
            onValidate,
            onToolChanged);

        _focusZone = EditorPaintFocusZone.Map;
        ApplyZoneChrome();
        RebuildContentList();
    }

    public void SyncStatus(string statusText, EditorValidationIndicator indicator)
    {
        _validationIndicator = indicator;
        if (_statusLabel is not null)
            _statusLabel.Text = statusText;
        if (_validationDot is not null)
            _validationDot.FillColor = IndicatorColor(indicator);
    }

    public void CycleFocusZone(int direction)
    {
        var next = ((int)_focusZone + direction + 3) % 3;
        SetFocusZone((EditorPaintFocusZone)next);
    }

    public void SetFocusZone(EditorPaintFocusZone zone)
    {
        _focusZone = zone;
        _tabsFocused = false;
        ApplyZoneChrome();
        if (zone == EditorPaintFocusZone.Content)
            FocusContentList();
        else if (zone == EditorPaintFocusZone.Tools && _toolEntries.Count > 0)
        {
            _toolFocusIndex = Math.Clamp(_toolFocusIndex, 0, _toolEntries.Count - 1);
            GumFocusableButtonList.ApplyFocus(_toolEntries, ref _toolFocusIndex);
        }
    }

    public void HandlePanelInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_focusZone == EditorPaintFocusZone.Content)
        {
            HandleContentInput(commands, elapsedSeconds);
            return;
        }

        if (_focusZone == EditorPaintFocusZone.Tools)
        {
            GumFocusableButtonList.HandleVerticalInput(
                commands,
                _toolEntries,
                ref _toolFocusIndex,
                _navigateRepeat,
                elapsedSeconds);
        }
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _leftPanel = null;
        _rightPanel = null;
        _contentListHost = null;
        _statusLabel = null;
        _validationDot = null;
        _contentEntries.Clear();
        _toolEntries.Clear();
        _tabEntries.Clear();
        _navigateRepeat.Reset();
        _contentFocusIndex = 0;
        _toolFocusIndex = 0;
        _tabFocusIndex = 0;
        _tabsFocused = false;
        _focusZone = EditorPaintFocusZone.Map;
        _contentTab = EditorContentTab.Terrain;
        _validationIndicator = EditorValidationIndicator.Idle;
        _tool = null;
        _textures = null;
        _catalog = null;
        _onToolChanged = null;
    }

    private void HandleContentInput(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_tabsFocused)
        {
            if (commands.WasPressed(GameCommand.NavigateDown))
            {
                _tabsFocused = false;
                FocusContentList();
                return;
            }

            var before = _tabFocusIndex;
            GumFocusableButtonList.HandleHorizontalInput(
                commands,
                _tabEntries,
                ref _tabFocusIndex,
                _navigateRepeat,
                elapsedSeconds);
            if (_tabFocusIndex != before)
                SelectContentTab((EditorContentTab)_tabFocusIndex);
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateUp) && _contentFocusIndex <= 0)
        {
            _tabsFocused = true;
            if (_tabEntries.Count > 0)
                GumFocusableButtonList.ApplyFocus(_tabEntries, ref _tabFocusIndex);
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateLeft) || commands.WasPressed(GameCommand.NavigateRight))
        {
            _tabsFocused = true;
            if (commands.WasPressed(GameCommand.NavigateLeft))
                CycleContentTab(-1);
            else
                CycleContentTab(+1);
            if (_tabEntries.Count > 0)
            {
                _tabFocusIndex = (int)_contentTab;
                GumFocusableButtonList.ApplyFocus(_tabEntries, ref _tabFocusIndex);
            }

            return;
        }

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _contentEntries,
            ref _contentFocusIndex,
            _navigateRepeat,
            elapsedSeconds);
    }

    private void CycleContentTab(int direction)
    {
        var next = ((int)_contentTab + direction + 3) % 3;
        SelectContentTab((EditorContentTab)next);
    }

    private void SelectContentTab(EditorContentTab tab)
    {
        _contentTab = tab;
        _tabFocusIndex = (int)tab;
        RebuildContentList();
        FocusContentList();
    }

    private void FocusContentList()
    {
        if (_contentEntries.Count == 0)
            return;
        _contentFocusIndex = Math.Clamp(_contentFocusIndex, 0, _contentEntries.Count - 1);
        GumFocusableButtonList.ApplyFocus(_contentEntries, ref _contentFocusIndex);
    }

    private void ApplyZoneChrome()
    {
        TintPanel(_leftPanel, _focusZone == EditorPaintFocusZone.Content);
        TintPanel(_rightPanel, _focusZone == EditorPaintFocusZone.Tools);
    }

    private static void TintPanel(Panel? panel, bool focused)
    {
        if (panel is null)
            return;

        foreach (var child in panel.Visual.Children)
        {
            if (child is not RectangleRuntime background)
                continue;
            background.FillColor = focused
                ? new Color(28, 42, 64, 245)
                : UiColors.EditorPanel;
            break;
        }
    }

    private static Panel CreateSidePanel(Anchor anchor, float height, out Panel stack)
    {
        var panel = new Panel();
        panel.Anchor(anchor);
        GumUiLayout.SetAbsoluteWidth(panel, SidePanelWidth);
        panel.Visual.Height = height;
        panel.Visual.HeightUnits = DimensionUnitType.Absolute;
        panel.Visual.HasEvents = true;
        GumUiLayout.AddSolidBackground(panel, UiColors.EditorPanel);

        stack = GumUiLayout.CreateVerticalStackPanel(spacing: 4f, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(stack);
        stack.Visual.Y = 6f;
        stack.Visual.HasEvents = false;
        panel.AddChild(stack);
        return panel;
    }

    private void BuildContentChrome(Panel stack, float panelHeight)
    {
        AddHeader(stack, "Content");

        var tabHost = new Panel();
        GumUiLayout.FillParentWidth(tabHost);
        tabHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.AddChild(tabHost);

        var tabButtons = new List<Button>();
        AddTabButton(tabButtons, "Terrain", EditorContentTab.Terrain);
        AddTabButton(tabButtons, "Buildings", EditorContentTab.Buildings);
        AddTabButton(tabButtons, "Units", EditorContentTab.Units);
        GumUiLayout.LayoutAdaptiveButtonRows(tabHost, tabButtons, availableWidth: SidePanelWidth - 16f, spacing: 4f);

        var listHeight = Math.Max(80f, panelHeight - 72f);
        var scroll = new ScrollViewer();
        GumUiLayout.FillParentWidth(scroll);
        scroll.Visual.Height = listHeight;
        scroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        scroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        scroll.InnerPanel.Width = 0;
        scroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        GumScrollViewerChrome.DisableScrollChromeFocus(scroll);
        stack.AddChild(scroll);

        _contentListHost = new Panel();
        _contentListHost.Visual.HasEvents = false;
        _contentListHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _contentListHost.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _contentListHost.Visual.StackSpacing = 3f;
        GumUiLayout.FillParentWidth(_contentListHost);
        scroll.AddChild(_contentListHost);
    }

    private void AddTabButton(List<Button> tabButtons, string text, EditorContentTab tab)
    {
        var button = new Button { Text = text };
        button.Click += (_, _) =>
        {
            _tabFocusIndex = (int)tab;
            SetFocusZone(EditorPaintFocusZone.Content);
            SelectContentTab(tab);
        };
        tabButtons.Add(button);
        _tabEntries.Add((button, () => SelectContentTab(tab)));
    }

    private Panel CreateIconButtonRow(Panel parent, string caption, Action onClick)
    {
        var row = new Panel();
        GumUiLayout.FillParentWidth(row);
        row.Visual.Height = IconSize + 8f;
        row.Visual.HeightUnits = DimensionUnitType.Absolute;
        parent.AddChild(row);

        var button = new Button { Text = "      " + caption };
        button.Dock(Dock.Fill);
        row.AddChild(button);
        _contentEntries.Add((button, onClick));
        var index = _contentEntries.Count - 1;
        button.Click += (_, _) =>
        {
            _contentFocusIndex = index;
            _tabsFocused = false;
            SetFocusZone(EditorPaintFocusZone.Content);
            onClick();
        };
        return row;
    }

    private void RebuildContentList()
    {
        if (_contentListHost is null || _tool is null || _textures is null || _catalog is null || _onToolChanged is null)
            return;

        _contentListHost.Visual.Children.Clear();
        _contentEntries.Clear();
        _contentFocusIndex = 0;

        switch (_contentTab)
        {
            case EditorContentTab.Buildings:
                PopulateBuildings(_contentListHost, _tool, _textures, _onToolChanged);
                break;
            case EditorContentTab.Units:
                PopulateUnits(_contentListHost, _tool, _textures, _catalog, _onToolChanged);
                break;
            default:
                PopulateTerrain(_contentListHost, _tool, _textures, _onToolChanged);
                break;
        }
    }

    private void PopulateTerrain(
        Panel host,
        EditorPaintToolState tool,
        MatchTextureAtlas textures,
        Action onToolChanged)
    {
        AddIconRow(host, "Grass", textures.Grass, () => { tool.SelectTerrain(TerrainKind.Grass); onToolChanged(); });
        AddIconRow(host, "Road", textures.Road, () => { tool.SelectTerrain(TerrainKind.Road); onToolChanged(); });
        AddIconRow(host, "Forest", textures.Forest, () => { tool.SelectTerrain(TerrainKind.Forest); onToolChanged(); });
        AddIconRow(host, "Mountain", textures.Mountain, () => { tool.SelectTerrain(TerrainKind.Mountain); onToolChanged(); });
        AddIconRow(host, "Water", textures.Water, () => { tool.SelectTerrain(TerrainKind.Water); onToolChanged(); });
        AddIconRow(host, "Bridge", textures.Bridge, () => { tool.SelectTerrain(TerrainKind.Bridge); onToolChanged(); });
    }

    private void PopulateBuildings(
        Panel host,
        EditorPaintToolState tool,
        MatchTextureAtlas textures,
        Action onToolChanged)
    {
        var castle = ContentId.Parse("vanilla/castle");
        var village = ContentId.Parse("vanilla/village");
        AddTeamIconRow(host, "Castle", textures.Building(castle), () =>
        {
            tool.SelectBuilding(castle);
            onToolChanged();
        });
        AddTeamIconRow(host, "Village", textures.Building(village), () =>
        {
            tool.SelectBuilding(village, "intact");
            onToolChanged();
        });
        AddTeamIconRow(host, "Village (ruined)", textures.Building(village, isRuined: true), () =>
        {
            tool.SelectBuilding(village, "ruined");
            onToolChanged();
        });
        AddIconRow(host, "Gravestone", textures.Gravestone, () =>
        {
            tool.SelectGravestone();
            onToolChanged();
        });
    }

    private void PopulateUnits(
        Panel host,
        EditorPaintToolState tool,
        MatchTextureAtlas textures,
        MatchContentCatalog catalog,
        Action onToolChanged)
    {
        foreach (var unitId in catalog.ListUnitTypeIdsForEditor())
        {
            var captured = unitId;
            AddTeamIconRow(host, Humanize(captured.LocalId), textures.Unit(captured), () =>
            {
                tool.SelectUnit(captured);
                onToolChanged();
            });
        }
    }

    private void FillToolsPanel(
        Panel stack,
        float panelHeight,
        EditorPaintToolState tool,
        Action onSave,
        Action onScript,
        Action onBack,
        Action onUndo,
        Action onRedo,
        Action onValidate,
        Action onToolChanged)
    {
        AddHeader(stack, "Tools");

        var listHeight = Math.Max(80f, panelHeight - 36f);
        var scroll = new ScrollViewer();
        GumUiLayout.FillParentWidth(scroll);
        scroll.Visual.Height = listHeight;
        scroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        scroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        scroll.InnerPanel.Width = 0;
        scroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        GumScrollViewerChrome.DisableScrollChromeFocus(scroll);
        stack.AddChild(scroll);

        var toolsHost = new Panel();
        toolsHost.Visual.HasEvents = false;
        toolsHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        toolsHost.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        toolsHost.Visual.StackSpacing = 3f;
        GumUiLayout.FillParentWidth(toolsHost);
        scroll.AddChild(toolsHost);

        AddPlainButton(toolsHost, "Place (Add)", () =>
        {
            tool.Mode = EditorPaintMode.Place;
            onToolChanged();
        }, _toolEntries);
        AddPlainButton(toolsHost, "Erase (Delete)", () =>
        {
            tool.Mode = EditorPaintMode.Erase;
            onToolChanged();
        }, _toolEntries);

        AddHeader(toolsHost, "Owner");
        AddPlainButton(toolsHost, "Neutral", () =>
        {
            tool.SelectOwner(null);
            onToolChanged();
        }, _toolEntries);
        for (var slot = 0; slot < EditorPaintToolState.PlayerSlotCount; slot++)
        {
            var captured = slot;
            AddPlainButton(toolsHost, PlayerDisplayNames.Number(captured), () =>
            {
                tool.SelectOwner(captured);
                onToolChanged();
            }, _toolEntries);
        }

        AddHeader(toolsHost, "History");
        AddPlainButton(toolsHost, "Undo (Ctrl+Z)", onUndo, _toolEntries);
        AddPlainButton(toolsHost, "Redo (Ctrl+Y)", onRedo, _toolEntries);
        AddPlainButton(toolsHost, "Validate", onValidate, _toolEntries);
        AddPlainButton(toolsHost, "Save", onSave, _toolEntries);
        AddPlainButton(toolsHost, "Script", onScript, _toolEntries);
        AddPlainButton(toolsHost, "Back", onBack, _toolEntries);
        AddHeader(toolsHost, "LB/RB · Q/E");
    }

    private static Color IndicatorColor(EditorValidationIndicator indicator) => indicator switch
    {
        EditorValidationIndicator.Ok => new Color(70, 200, 110),
        EditorValidationIndicator.Warning => new Color(230, 190, 60),
        EditorValidationIndicator.Error => new Color(230, 70, 70),
        _ => new Color(70, 140, 255),
    };

    private void AddHeader(Panel parent, string text)
    {
        var label = new Label { Text = text };
        label.Visual.HasEvents = false;
        GumUiLayout.FillParentWidth(label);
        parent.AddChild(label);
    }

    private void AddPlainButton(
        Panel parent,
        string text,
        Action onClick,
        List<(Button Button, Action Activate)> focusList)
    {
        var button = new Button { Text = text };
        GumUiLayout.FillParentWidth(button);
        parent.AddChild(button);
        focusList.Add((button, onClick));
        var index = focusList.Count - 1;
        button.Click += (_, _) =>
        {
            _toolFocusIndex = index;
            SetFocusZone(EditorPaintFocusZone.Tools);
            onClick();
        };
    }

    private void AddIconRow(Panel parent, string caption, Texture2D texture, Action onClick)
    {
        var row = CreateIconButtonRow(parent, caption, onClick);
        var icon = new SpriteRuntime
        {
            Texture = texture,
            Width = IconSize,
            Height = IconSize,
            X = 4f,
            Y = 4f,
        };
        icon.WidthUnits = DimensionUnitType.Absolute;
        icon.HeightUnits = DimensionUnitType.Absolute;
        row.AddChild(icon);
    }

    private void AddTeamIconRow(Panel parent, string caption, TeamSprite sprite, Action onClick)
    {
        var row = CreateIconButtonRow(parent, caption, onClick);
        var baseSprite = new SpriteRuntime
        {
            Texture = sprite.Base,
            Width = IconSize,
            Height = IconSize,
            X = 4f,
            Y = 4f,
        };
        baseSprite.WidthUnits = DimensionUnitType.Absolute;
        baseSprite.HeightUnits = DimensionUnitType.Absolute;
        row.AddChild(baseSprite);
    }

    private static string Humanize(string localId) =>
        string.IsNullOrWhiteSpace(localId)
            ? localId
            : char.ToUpperInvariant(localId[0]) + localId[1..];

    private static bool IsOverPanel(object over, Panel? panel)
    {
        if (panel is null)
            return false;
        if (ReferenceEquals(over, panel) || ReferenceEquals(over, panel.Visual))
            return true;
        if (over is GraphicalUiElement visual)
            return GumScrollListLayout.IsDescendantOf(visual, panel.Visual);
        if (over is FrameworkElement element && element.Visual is not null)
            return GumScrollListLayout.IsDescendantOf(element.Visual, panel.Visual);
        return false;
    }
}
