using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>
/// Shared two-column editor form: Settings scroll (left) + Menu (right).
/// Callers own domain rebuild / button wiring.
/// </summary>
public static class EditorTwoColumnFormShell
{
    public const float DefaultStackSpacing = 6f;
    public const float DefaultSettingsListMinHeight = 260f;
    public const float DefaultTextFieldHeight = 36f;
    public const float DefaultMinScrollViewport = 120f;

    public sealed class BuiltShell
    {
        public required Panel RootPanel { get; init; }

        public required Panel Shell { get; init; }

        public required Panel SettingsHost { get; init; }

        public required ScrollViewer SettingsScroll { get; init; }

        public required Panel MenuHost { get; init; }

        /// <summary>Set when the menu column scrolls; otherwise the menu is a fixed stack.</summary>
        public ScrollViewer? MenuScroll { get; init; }

        public required float ListHeight { get; init; }
    }

    public static BuiltShell Build(
        string headingText,
        string hintText,
        float maxShellWidthPixels = 760f,
        float settingsWidthPercent = 62f,
        float menuWidthPercent = 36f,
        float settingsListMinHeight = DefaultSettingsListMinHeight,
        string settingsHeader = "Settings",
        string menuHeader = "Menu",
        bool scrollMenuColumn = false)
    {
        var rootPanel = new Panel();
        rootPanel.Dock(Dock.Fill);
        rootPanel.AddToRoot();

        var body = new Panel();
        body.Dock(Dock.Fill);
        rootPanel.AddChild(body);

        var shell = new Panel();
        GumUiLayout.CenterHorizontallyInParent(shell);
        shell.Visual.Y = 12f;
        GumUiLayout.SetBoundedWidth(shell, maxPixels: maxShellWidthPixels, parentPercent: 96f);
        var canvasHeight = Math.Max(320f, GumService.Default.CanvasHeight);
        var shellHeight = Math.Max(320f, canvasHeight - 24f);
        const float topChrome = 10f + 26f + 26f + DefaultStackSpacing * 3f;
        const float bottomChrome = 10f;
        const float columnHeader = 26f + 4f;
        var listHeight = Math.Max(settingsListMinHeight, shellHeight - topChrome - bottomChrome - columnHeader);
        GumUiLayout.SetAbsoluteHeight(shell, topChrome + bottomChrome + columnHeader + listHeight);
        body.AddChild(shell);
        GumUiLayout.AddSolidBackground(shell, UiColors.EditorPanel);

        var rootStack = GumUiLayout.CreateVerticalStackPanel(spacing: DefaultStackSpacing, widthPercent: 94f);
        GumUiLayout.CenterHorizontallyInParent(rootStack);
        shell.AddChild(rootStack);
        GumUiLayout.AddVerticalSpacer(rootStack, 10f);

        var heading = new Label { Text = headingText };
        GumUiLayout.FillParentWidth(heading);
        rootStack.AddChild(heading);

        var hint = new Label { Text = hintText };
        GumUiLayout.FillParentWidth(hint);
        rootStack.AddChild(hint);

        var columns = new Panel();
        GumUiLayout.FillParentWidth(columns);
        columns.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        columns.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        columns.Visual.StackSpacing = 10f;
        rootStack.AddChild(columns);

        var leftColumn = CreateScrollColumn(columns, settingsHeader, listHeight, out var settingsHost, out var settingsScroll);
        ScrollViewer? menuScroll = null;
        Panel menuHost;
        Panel rightColumn;
        if (scrollMenuColumn)
            rightColumn = CreateScrollColumn(columns, menuHeader, listHeight, out menuHost, out menuScroll);
        else
            rightColumn = CreatePlainColumn(columns, menuHeader, listHeight, out menuHost);
        GumUiLayout.SetWidthPercent(leftColumn, settingsWidthPercent);
        GumUiLayout.SetWidthPercent(rightColumn, menuWidthPercent);

        GumUiLayout.AddVerticalSpacer(rootStack, 10f);

        return new BuiltShell
        {
            RootPanel = rootPanel,
            Shell = shell,
            SettingsHost = settingsHost,
            SettingsScroll = settingsScroll,
            MenuHost = menuHost,
            MenuScroll = menuScroll,
            ListHeight = listHeight,
        };
    }

    public static Panel CreateScrollColumn(
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
        host.Visual.StackSpacing = DefaultStackSpacing;
        GumUiLayout.FillParentWidth(host);
        scroll.AddChild(host);
        return column;
    }

    public static Panel CreatePlainColumn(
        Panel columns,
        string headerText,
        float listHeight,
        out Panel host)
    {
        var column = new Panel();
        column.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        column.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        column.Visual.StackSpacing = 4f;
        columns.AddChild(column);

        var header = new Label { Text = headerText };
        GumUiLayout.FillParentWidth(header);
        column.AddChild(header);

        host = new Panel();
        GumUiLayout.FillParentWidth(host);
        host.Visual.Height = listHeight;
        host.Visual.HeightUnits = DimensionUnitType.Absolute;
        host.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        host.Visual.StackSpacing = 6f;
        column.AddChild(host);
        return column;
    }

    public static void AddTextField(Panel parent, string caption, string initialText, out TextBox textBox)
    {
        var label = new Label { Text = caption };
        GumUiLayout.FillParentWidth(label);
        parent.AddChild(label);
        textBox = new TextBox { Text = initialText };
        GumUiLayout.FillParentWidth(textBox);
        GumUiLayout.SetAbsoluteHeight(textBox, DefaultTextFieldHeight);
        EditorTextFieldStyle.Apply(textBox);
        parent.AddChild(textBox);
    }

    /// <param name="allowDpadColumnSwitch">
    /// When false, only LB/RB (Focus*Region / Zoom) switch columns — D-pad Left/Right stay free for steppers.
    /// </param>
    public static bool TrySwitchTwoZones(
        IGameCommandSource commands,
        bool allowDpadColumnSwitch,
        int currentZone,
        int settingsZone,
        int menuZone,
        Action<int> setZone)
    {
        var toMenu = commands.WasPressed(GameCommand.FocusNextRegion)
            || commands.WasPressed(GameCommand.ZoomIn);
        var toSettings = commands.WasPressed(GameCommand.FocusPreviousRegion)
            || commands.WasPressed(GameCommand.ZoomOut);

        if (allowDpadColumnSwitch)
        {
            toMenu = toMenu || commands.WasPressed(GameCommand.NavigateRight);
            toSettings = toSettings || commands.WasPressed(GameCommand.NavigateLeft);
        }

        if (toMenu && currentZone == settingsZone)
        {
            setZone(menuZone);
            return true;
        }

        if (toSettings && currentZone == menuZone)
        {
            setZone(settingsZone);
            return true;
        }

        return (toMenu && currentZone == menuZone) || (toSettings && currentZone == settingsZone);
    }
}
