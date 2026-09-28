using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace TinyTBS.Engine.GumLayout;

/// <summary>
/// Button visual whose focus cue is a full outline around the control (not the V3 bottom bar).
/// Built from four filled edge bars so corners stay solid and nothing is clipped outside bounds.
/// </summary>
internal sealed class OutlineFocusButtonVisual : ButtonVisual
{
    private readonly RectangleRuntime _top;
    private readonly RectangleRuntime _bottom;
    private readonly RectangleRuntime _left;
    private readonly RectangleRuntime _right;

    public OutlineFocusButtonVisual()
        : base(fullInstantiation: true, tryCreateFormsObject: false)
    {
        // Default FocusedIndicator is a nine-slice bar; hide it.
        FocusedIndicator.Visible = false;
        FocusedIndicator.Width = 0;
        FocusedIndicator.Height = 0;
        FocusedIndicator.HasEvents = false;

        var thickness = GumFocusOutline.ThicknessPixels;
        _top = CreateEdgeBar("FocusOutlineTop");
        _bottom = CreateEdgeBar("FocusOutlineBottom");
        _left = CreateEdgeBar("FocusOutlineLeft");
        _right = CreateEdgeBar("FocusOutlineRight");

        // Top / bottom: full width, fixed thickness.
        LayoutHorizontalEdge(_top, fromTop: true, thickness);
        LayoutHorizontalEdge(_bottom, fromTop: false, thickness);

        // Left / right: full height, fixed thickness (overlap corners with top/bottom).
        LayoutVerticalEdge(_left, fromLeft: true, thickness);
        LayoutVerticalEdge(_right, fromLeft: false, thickness);

        AddChild(_top);
        AddChild(_bottom);
        AddChild(_left);
        AddChild(_right);

        WrapFocusState(States.Enabled, focused: false);
        WrapFocusState(States.Disabled, focused: false);
        WrapFocusState(States.Highlighted, focused: false);
        WrapFocusState(States.Pushed, focused: false);
        WrapFocusState(States.Focused, focused: true);
        WrapFocusState(States.HighlightedFocused, focused: true);
        WrapFocusState(States.DisabledFocused, focused: true);
    }

    private void WrapFocusState(StateSave state, bool focused)
    {
        var previous = state.Apply;
        state.Apply = () =>
        {
            previous?.Invoke();
            FocusedIndicator.Visible = false;
            SetOutlineVisible(focused);
            if (focused)
                SetOutlineColor(FocusedIndicatorColor);
        };
    }

    private void SetOutlineVisible(bool visible)
    {
        _top.Visible = visible;
        _bottom.Visible = visible;
        _left.Visible = visible;
        _right.Visible = visible;
    }

    private void SetOutlineColor(Color color)
    {
        _top.FillColor = color;
        _bottom.FillColor = color;
        _left.FillColor = color;
        _right.FillColor = color;
    }

    private static RectangleRuntime CreateEdgeBar(string name)
    {
        var bar = new RectangleRuntime();
        bar.Name = name;
        bar.IsFilled = true;
        bar.StrokeWidth = 0;
        bar.Visible = false;
        return bar;
    }

    private static void LayoutHorizontalEdge(RectangleRuntime bar, bool fromTop, float thickness)
    {
        bar.X = 0;
        bar.XUnits = GeneralUnitType.PixelsFromMiddle;
        bar.XOrigin = HorizontalAlignment.Center;
        bar.Width = 0;
        bar.WidthUnits = DimensionUnitType.RelativeToParent;
        bar.Height = thickness;
        bar.HeightUnits = DimensionUnitType.Absolute;
        bar.Y = 0;
        if (fromTop)
        {
            bar.YUnits = GeneralUnitType.PixelsFromSmall;
            bar.YOrigin = VerticalAlignment.Top;
        }
        else
        {
            bar.YUnits = GeneralUnitType.PixelsFromLarge;
            bar.YOrigin = VerticalAlignment.Bottom;
        }
    }

    private static void LayoutVerticalEdge(RectangleRuntime bar, bool fromLeft, float thickness)
    {
        bar.Y = 0;
        bar.YUnits = GeneralUnitType.PixelsFromMiddle;
        bar.YOrigin = VerticalAlignment.Center;
        bar.Height = 0;
        bar.HeightUnits = DimensionUnitType.RelativeToParent;
        bar.Width = thickness;
        bar.WidthUnits = DimensionUnitType.Absolute;
        bar.X = 0;
        if (fromLeft)
        {
            bar.XUnits = GeneralUnitType.PixelsFromSmall;
            bar.XOrigin = HorizontalAlignment.Left;
        }
        else
        {
            bar.XUnits = GeneralUnitType.PixelsFromLarge;
            bar.XOrigin = HorizontalAlignment.Right;
        }
    }
}
