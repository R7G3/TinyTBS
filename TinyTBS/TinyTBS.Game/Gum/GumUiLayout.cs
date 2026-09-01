using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace TinyTBS.Game.Gum;

/// <summary>
/// Shared helpers for responsive Gum layout (units, percentages, anchors).
/// </summary>
internal static class GumUiLayout
{
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
    /// Pins a control to the bottom-right of its parent.
    /// Gum expects negative <see cref="FrameworkElement.X"/> / <see cref="FrameworkElement.Y"/>
    /// offsets after <see cref="FrameworkElement.Anchor"/> — not percentage units on Visual.
    /// </summary>
    public static void PinToBottomRight(
        FrameworkElement element,
        float insetPixels,
        float widthPercent)
    {
        element.Anchor(Anchor.BottomRight);
        element.X = -insetPixels;
        element.Y = -insetPixels;
        element.Visual.Width = widthPercent;
        element.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;
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