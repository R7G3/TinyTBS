using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;

namespace TinyTBS.Engine.GumLayout;

/// <summary>
/// Measure stacked list children and scroll a focused row into view (New Game / Content menus).
/// </summary>
public static class GumScrollListLayout
{
    public const float DefaultRowHeightFallback = 44f;
    public const float DefaultScrollIntoViewMargin = 12f;
    public const float DefaultListBottomPadding = 18f;

    public static float MeasureVisualHeight(
        GraphicalUiElement visual,
        float rowHeightFallback = DefaultRowHeightFallback)
    {
        if (visual.AbsoluteHeight > 1f)
            return visual.AbsoluteHeight;
        if (visual.Height > 1f)
            return visual.Height;
        return rowHeightFallback;
    }

    public static bool IsDescendantOf(GraphicalUiElement node, GraphicalUiElement ancestor)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
                return true;
            current = current.Parent;
        }

        return false;
    }

    public static float ResolveScrollViewportHeight(
        ScrollViewer listScroll,
        float minHeightFallback)
    {
        ArgumentNullException.ThrowIfNull(listScroll);

        if (listScroll.Visual is ScrollViewerVisual visual
            && visual.ClipContainerInstance is not null
            && visual.ClipContainerInstance.AbsoluteHeight > 1f)
        {
            return visual.ClipContainerInstance.AbsoluteHeight;
        }

        var outer = listScroll.Visual.AbsoluteHeight > 1f
            ? listScroll.Visual.AbsoluteHeight
            : listScroll.Visual.Height;
        const float scrollViewerChromeHeight = 36f;
        return outer > scrollViewerChromeHeight
            ? outer - scrollViewerChromeHeight
            : Math.Max(outer, minHeightFallback);
    }

    /// <summary>Sum of child heights + stack spacing (uses fallbacks before Gum layout).</summary>
    public static float MeasureStackContentHeight(
        Panel listPanel,
        float stackSpacing,
        float rowHeightFallback = DefaultRowHeightFallback)
    {
        ArgumentNullException.ThrowIfNull(listPanel);

        if (listPanel.Visual.Children is null || listPanel.Visual.Children.Count == 0)
            return rowHeightFallback;

        var total = 0f;
        for (var index = 0; index < listPanel.Visual.Children.Count; index++)
        {
            total += MeasureVisualHeight(listPanel.Visual.Children[index], rowHeightFallback);
            if (index < listPanel.Visual.Children.Count - 1)
                total += stackSpacing;
        }

        return total;
    }

    public static bool TryMeasureItemInList(
        Panel listPanel,
        GraphicalUiElement focusedVisual,
        float stackSpacing,
        out float itemTop,
        out float itemHeight,
        float rowHeightFallback = DefaultRowHeightFallback)
    {
        ArgumentNullException.ThrowIfNull(listPanel);
        ArgumentNullException.ThrowIfNull(focusedVisual);

        itemTop = 0f;
        itemHeight = rowHeightFallback;
        if (listPanel.Visual.Children is null)
            return false;

        // Stack layouts often keep child.Y at 0 — walk siblings and sum heights (do this first).
        var found = false;
        for (var index = 0; index < listPanel.Visual.Children.Count; index++)
        {
            var child = listPanel.Visual.Children[index];
            var childHeight = MeasureVisualHeight(child, rowHeightFallback);
            if (ReferenceEquals(child, focusedVisual) || IsDescendantOf(focusedVisual, child))
            {
                itemHeight = MeasureVisualHeight(focusedVisual, rowHeightFallback);
                if (itemHeight <= 1f || childHeight > itemHeight)
                    itemHeight = childHeight;
                found = true;
                break;
            }

            itemTop += childHeight + stackSpacing;
        }

        if (found)
            return true;

        // AbsoluteTop delta when sibling walk missed (unusual nesting).
        return TryMeasureViaAbsoluteTop(listPanel.Visual, focusedVisual, out itemTop, out itemHeight, rowHeightFallback);
    }

    public static bool TryMeasureViaAbsoluteTop(
        GraphicalUiElement listRoot,
        GraphicalUiElement item,
        out float itemTop,
        out float itemHeight,
        float rowHeightFallback = DefaultRowHeightFallback)
    {
        itemTop = 0f;
        itemHeight = MeasureVisualHeight(item, rowHeightFallback);

        var rootTop = listRoot.AbsoluteTop;
        var itemAbsoluteTop = item.AbsoluteTop;
        if (float.IsNaN(rootTop) || float.IsNaN(itemAbsoluteTop))
            return false;

        itemTop = itemAbsoluteTop - rootTop;
        if (itemTop < -1f)
            return false;

        if (item.Parent is GraphicalUiElement row && !ReferenceEquals(row, listRoot))
        {
            var rowHeight = MeasureVisualHeight(row, rowHeightFallback);
            if (rowHeight > itemHeight)
                itemHeight = rowHeight;
            // Align to the containing row top when the focused visual is nested.
            var rowTop = row.AbsoluteTop - rootTop;
            if (!float.IsNaN(rowTop) && rowTop >= 0f)
                itemTop = rowTop;
        }

        return true;
    }

    public static void EnsureFocusedRowVisible(
        ScrollViewer listScroll,
        Panel listPanel,
        GraphicalUiElement focusedVisual,
        int listFocusStartIndex,
        int listFocusCount,
        int focusIndex,
        float stackSpacing,
        float minViewportHeight,
        float scrollIntoViewMargin = DefaultScrollIntoViewMargin,
        float listBottomPadding = DefaultListBottomPadding,
        float rowHeightFallback = DefaultRowHeightFallback)
    {
        ArgumentNullException.ThrowIfNull(listScroll);
        ArgumentNullException.ThrowIfNull(listPanel);
        ArgumentNullException.ThrowIfNull(focusedVisual);

        if (listFocusStartIndex < 0
            || focusIndex < listFocusStartIndex
            || focusIndex >= listFocusStartIndex + listFocusCount)
        {
            return;
        }

        // Topmost list focusable: jump to absolute top so non-focusable headers above stay visible.
        if (focusIndex == listFocusStartIndex)
        {
            listScroll.VerticalScrollBarValue = 0f;
            return;
        }

        if (!TryMeasureItemInList(
                listPanel,
                focusedVisual,
                stackSpacing,
                out var itemTop,
                out var itemHeight,
                rowHeightFallback))
        {
            var rowIndex = focusIndex - listFocusStartIndex;
            itemTop = rowIndex * (rowHeightFallback + stackSpacing);
            itemHeight = rowHeightFallback;
        }

        var itemBottom = itemTop + itemHeight;
        var current = listScroll.VerticalScrollBarValue;
        var viewHeight = ResolveScrollViewportHeight(listScroll, minViewportHeight);
        if (viewHeight <= 1f)
            return;

        var viewBottom = current + viewHeight;

        if (itemTop < current + scrollIntoViewMargin)
            listScroll.VerticalScrollBarValue = Math.Max(0f, itemTop - scrollIntoViewMargin);
        else if (itemBottom + listBottomPadding > viewBottom - scrollIntoViewMargin)
            listScroll.VerticalScrollBarValue = Math.Max(
                0f,
                itemBottom + listBottomPadding - viewHeight + scrollIntoViewMargin);
    }
}
