using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace TinyTBS.Engine.GumLayout;

/// <summary>
/// Shared helpers for responsive Gum layout (units, percentages, anchors).
/// </summary>
public static class GumUiLayout
{
    /// <summary>
    /// Solid fill behind panel children. Events are off so the rectangle does not steal clicks.
    /// </summary>
    public static RectangleRuntime AddSolidBackground(Panel panel, Color color)
    {
        var background = new RectangleRuntime();
        background.Dock(Dock.Fill);
        background.FillColor = color;
        background.IsFilled = true;
        // Added first so later siblings (buttons/labels) sit above for hit-testing.
        panel.AddChild(background);
        return background;
    }

    public static void FillParentWidth(FrameworkElement element, float horizontalInset = 0)
    {
        element.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        element.Visual.Width = horizontalInset;
    }

    public static void SetWidthPercent(FrameworkElement element, float percent)
    {
        element.Visual.Width = percent;
        element.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;
    }

    public static void SetAbsoluteWidth(FrameworkElement element, float pixels)
    {
        element.Visual.Width = pixels;
        element.Visual.WidthUnits = DimensionUnitType.Absolute;
    }

    /// <summary>
    /// Tracks a parent percentage on narrow windows, but never exceeds
    /// <paramref name="maxPixels"/> so menus stay content-sized on wide / fullscreen.
    /// </summary>
    public static void SetBoundedWidth(
        FrameworkElement element,
        float maxPixels,
        float parentPercent = 92f)
    {
        element.Visual.Width = parentPercent;
        element.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;
        element.Visual.MaxWidth = maxPixels;
    }

    /// <summary>
    /// Same idea as <see cref="SetBoundedWidth"/> for height (percent of parent, capped in pixels).
    /// </summary>
    public static void SetBoundedHeight(
        FrameworkElement element,
        float maxPixels,
        float parentPercent = 92f)
    {
        element.Visual.Height = parentPercent;
        element.Visual.HeightUnits = DimensionUnitType.PercentageOfParent;
        element.Visual.MaxHeight = maxPixels;
    }

    public static void SetAbsoluteHeight(FrameworkElement element, float pixels)
    {
        element.Visual.Height = pixels;
        element.Visual.HeightUnits = DimensionUnitType.Absolute;
        element.Visual.MaxHeight = pixels;
    }

    public static void CenterInParent(FrameworkElement element, float xPercent = 50f, float yPercent = 50f)
    {
        element.Visual.X = xPercent;
        element.Visual.XUnits = GeneralUnitType.Percentage;
        element.Visual.XOrigin = HorizontalAlignment.Center;
        element.Visual.Y = yPercent;
        element.Visual.YUnits = GeneralUnitType.Percentage;
        element.Visual.YOrigin = VerticalAlignment.Center;
    }

    /// <summary>
    /// Centers horizontally in the parent at the top edge (safe for RelativeToChildren parents).
    /// Use stack children for vertical padding so parent height includes it.
    /// </summary>
    public static void CenterHorizontallyInParent(FrameworkElement element)
    {
        element.Visual.X = 50f;
        element.Visual.XUnits = GeneralUnitType.Percentage;
        element.Visual.XOrigin = HorizontalAlignment.Center;
        element.Y = 0;
        element.Visual.YOrigin = VerticalAlignment.Top;
    }

    /// <summary>Fixed-height spacer for vertical stacks (counts toward RelativeToChildren height).</summary>
    public static void AddVerticalSpacer(Panel stack, float heightPixels)
    {
        var spacer = new Panel();
        spacer.Visual.HasEvents = false;
        spacer.Visual.Height = heightPixels;
        spacer.Visual.HeightUnits = DimensionUnitType.Absolute;
        spacer.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        spacer.Visual.Width = 0;
        stack.AddChild(spacer);
    }

    /// <summary>
    /// Pins a control to the bottom-right of its parent.
    /// Gum expects negative <see cref="FrameworkElement.X"/> / <see cref="FrameworkElement.Y"/>
    /// offsets after <see cref="FrameworkElement.Anchor"/> — not percentage units on Visual.
    /// </summary>
    public static void PinToBottomRight(
        FrameworkElement element,
        float insetPixels,
        float widthPixels)
    {
        element.Anchor(Anchor.BottomRight);
        element.X = -insetPixels;
        element.Y = -insetPixels;
        SetAbsoluteWidth(element, widthPixels);
    }

    public static Panel CreateVerticalStackPanel(float spacing = 12f, float widthPercent = 92f)
    {
        var panel = new Panel();
        panel.Visual.Width = widthPercent;
        panel.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;
        panel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        panel.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        panel.Visual.StackSpacing = spacing;
        return panel;
    }
}
