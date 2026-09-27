using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;

namespace TinyTBS.Engine.GumLayout;

/// <summary>
/// Keeps Gum <see cref="ScrollViewer"/> chrome (bars / focused indicator) from stealing
/// menu focus driven by <c>GameCommand</c>. Safe for mouse scrolling (does not disable bars).
/// </summary>
public static class GumScrollViewerChrome
{
    public static void StealFocusFromScrollChrome(ScrollViewer? scrollViewer) =>
        ClearScrollBarFocus(scrollViewer);

    public static void DisableScrollChromeFocus(ScrollViewer scrollViewer)
    {
        ArgumentNullException.ThrowIfNull(scrollViewer);

        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return;

        if (visual.FocusedIndicator is not null)
            visual.FocusedIndicator.Visible = false;

        ClearScrollBarFocus(scrollViewer);
    }

    public static void ClearScrollBarFocus(ScrollViewer? scrollViewer)
    {
        if (scrollViewer?.Visual is not ScrollViewerVisual visual)
            return;

        if (scrollViewer.IsFocused)
            scrollViewer.IsFocused = false;

        ClearFormsFocus(visual.VerticalScrollBarInstance);
        ClearFormsFocus(visual.HorizontalScrollBarInstance);
    }

    public static bool IsScrollChromeFocused(ScrollViewer? scrollViewer)
    {
        if (scrollViewer is null)
            return false;

        if (scrollViewer.IsFocused)
            return true;

        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return false;

        return IsFormsFocused(visual.VerticalScrollBarInstance)
            || IsFormsFocused(visual.HorizontalScrollBarInstance);
    }

    private static bool IsFormsFocused(GraphicalUiElement? element) =>
        element is ScrollBarVisual { FormsControl.IsFocused: true };

    private static void ClearFormsFocus(GraphicalUiElement? element)
    {
        if (element is ScrollBarVisual { FormsControl.IsFocused: true } scrollBarVisual)
            scrollBarVisual.FormsControl.IsFocused = false;
    }
}
