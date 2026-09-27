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

    /// <summary>
    /// Lays out <paramref name="buttons"/> into one or more horizontal rows that fit
    /// <paramref name="availableWidth"/> (wraps to extra rows and shrinks widths as needed).
    /// <paramref name="host"/> becomes a vertical stack of row panels; buttons are reparented.
    /// </summary>
    public static void LayoutAdaptiveButtonRows(
        Panel host,
        IReadOnlyList<Button> buttons,
        float availableWidth,
        float spacing = 8f,
        float minButtonWidth = 72f,
        float preferredButtonWidth = 110f)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(buttons);

        host.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        host.Visual.StackSpacing = spacing;
        host.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        FillParentWidth(host);

        // Detach buttons before clearing rows so they are not disposed with old parents.
        foreach (var button in buttons)
            button.Visual.Parent = null;

        host.Visual.Children.Clear();

        if (buttons.Count == 0 || availableWidth <= 1f)
            return;

        var widthBudget = Math.Max(minButtonWidth, availableWidth);
        var columns = ResolveColumnCount(
            buttons.Count,
            widthBudget,
            spacing,
            minButtonWidth,
            preferredButtonWidth);

        var buttonWidth = (widthBudget - spacing * Math.Max(columns - 1, 0)) / columns;
        buttonWidth = Math.Clamp(buttonWidth, minButtonWidth, preferredButtonWidth);

        Panel? currentRow = null;
        var inRow = 0;
        for (var i = 0; i < buttons.Count; i++)
        {
            if (currentRow is null || inRow >= columns)
            {
                currentRow = new Panel();
                currentRow.Visual.HasEvents = false;
                currentRow.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
                currentRow.Visual.StackSpacing = spacing;
                currentRow.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
                FillParentWidth(currentRow);
                host.AddChild(currentRow);
                inRow = 0;
            }

            var button = buttons[i];
            SetAbsoluteWidth(button, buttonWidth);
            currentRow.AddChild(button);
            inRow++;
        }
    }

    private static int ResolveColumnCount(
        int buttonCount,
        float availableWidth,
        float spacing,
        float minButtonWidth,
        float preferredButtonWidth)
    {
        if (buttonCount <= 1)
            return 1;

        float RowWidth(int columns) =>
            columns * preferredButtonWidth + Math.Max(columns - 1, 0) * spacing;

        float MinRowWidth(int columns) =>
            columns * minButtonWidth + Math.Max(columns - 1, 0) * spacing;

        if (RowWidth(buttonCount) <= availableWidth || MinRowWidth(buttonCount) <= availableWidth)
            return buttonCount;

        // Prefer 2 columns on phone-like widths when possible; otherwise 1 column stack.
        for (var columns = Math.Min(buttonCount, 3); columns >= 1; columns--)
        {
            if (MinRowWidth(columns) <= availableWidth)
                return columns;
        }

        return 1;
    }
}
