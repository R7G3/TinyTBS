using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Hud;

public sealed class MatchStatusBarView
{
    private const float BarHeight = 48f;
    private const float LeftPadding = 14f;
    private const float OutlineOffset = 1f;

    private static readonly Color FillColor = Color.White;
    private static readonly Color OutlineColor = new(12, 14, 18, 230);

    private static readonly (int Dx, int Dy)[] OutlineOffsets =
    [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0), (1, 0),
        (-1, 1), (0, 1), (1, 1),
    ];

    private RectangleRuntime? _background;
    private Panel? _labelRow;
    private OutlinedStatusField? _playerField;
    private OutlinedStatusField? _goldField;
    private OutlinedStatusField? _incomeField;
    private OutlinedStatusField? _unitsField;
    private OutlinedStatusField? _turnField;

    public void Build(Panel root, GameplayHudViewModel hud)
    {
        var statusBar = new Panel();
        statusBar.Dock(Dock.Top);
        statusBar.Visual.Height = BarHeight;
        statusBar.Visual.HeightUnits = DimensionUnitType.Absolute;
        statusBar.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        statusBar.Visual.Width = 0;
        statusBar.Visual.HasEvents = false;
        root.AddChild(statusBar);

        _background = GumUiLayout.AddSolidBackground(statusBar, hud.StatusBarColor);

        _labelRow = new Panel();
        _labelRow.Dock(Dock.Fill);
        _labelRow.Visual.X = LeftPadding;
        _labelRow.Visual.Width = -LeftPadding;
        _labelRow.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        _labelRow.Visual.HasEvents = false;
        _labelRow.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        _labelRow.Visual.StackSpacing = ResolveLabelSpacing();
        statusBar.AddChild(_labelRow);

        _playerField = AddOutlinedField(_labelRow, hud.PlayerLabel);
        _goldField = AddOutlinedField(_labelRow, hud.GoldText);
        _incomeField = AddOutlinedField(_labelRow, hud.IncomeText);
        _unitsField = AddOutlinedField(_labelRow, hud.UnitsText);
        _turnField = AddOutlinedField(_labelRow, hud.TurnText);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        _playerField?.SetText(hud.PlayerLabel);
        _goldField?.SetText(hud.GoldText);
        _incomeField?.SetText(hud.IncomeText);
        _unitsField?.SetText(hud.UnitsText);
        _turnField?.SetText(hud.TurnText);
        if (_background is not null)
            _background.FillColor = hud.StatusBarColor;
        if (_labelRow is not null)
            _labelRow.Visual.StackSpacing = ResolveLabelSpacing();
    }

    private static OutlinedStatusField AddOutlinedField(Panel row, string text)
    {
        var host = new Panel();
        host.Visual.HasEvents = false;
        host.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
        host.Visual.Height = 0;
        host.Visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.AddChild(host);

        var outlines = new TextRuntime[OutlineOffsets.Length];
        for (var i = 0; i < OutlineOffsets.Length; i++)
        {
            var (dx, dy) = OutlineOffsets[i];
            outlines[i] = CreateStatusText(
                host,
                text,
                OutlineColor,
                pixelOffsetX: dx * OutlineOffset,
                pixelOffsetY: dy * OutlineOffset);
        }

        var fill = CreateStatusText(host, text, FillColor, pixelOffsetX: 0f, pixelOffsetY: 0f);
        return new OutlinedStatusField(fill, outlines);
    }

    private static TextRuntime CreateStatusText(
        Panel host,
        string text,
        Color color,
        float pixelOffsetX,
        float pixelOffsetY)
    {
        var runtime = new TextRuntime
        {
            Text = text,
            Color = color,
        };
        runtime.X = pixelOffsetX;
        runtime.XUnits = GeneralUnitType.PixelsFromSmall;
        runtime.XOrigin = HorizontalAlignment.Left;
        // Center in the status bar host (fills the 48px strip).
        runtime.Y = (BarHeight * 0.5f) + pixelOffsetY;
        runtime.YUnits = GeneralUnitType.PixelsFromSmall;
        runtime.YOrigin = VerticalAlignment.Center;
        runtime.WidthUnits = DimensionUnitType.RelativeToChildren;
        runtime.HeightUnits = DimensionUnitType.RelativeToChildren;
        host.AddChild(runtime);
        return runtime;
    }

    private static float ResolveLabelSpacing() =>
        GumService.Default.CanvasWidth is > 0 and < 520f ? 12f : 24f;

    private sealed class OutlinedStatusField
    {
        private readonly TextRuntime _fill;
        private readonly TextRuntime[] _outlines;

        public OutlinedStatusField(TextRuntime fill, TextRuntime[] outlines)
        {
            _fill = fill;
            _outlines = outlines;
        }

        public void SetText(string text)
        {
            _fill.Text = text;
            for (var i = 0; i < _outlines.Length; i++)
                _outlines[i].Text = text;
        }
    }
}
