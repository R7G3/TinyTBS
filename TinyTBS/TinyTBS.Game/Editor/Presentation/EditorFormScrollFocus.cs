using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Scroll a focused button into view inside editor form ScrollViewers.</summary>
internal static class EditorFormScrollFocus
{
    public static void AfterNavigate(
        ScrollViewer? listScroll,
        Panel? listPanel,
        IReadOnlyList<(Button Button, Action Activate)> entries,
        int listFocusStartIndex,
        int listFocusCount,
        int focusIndex,
        float stackSpacing,
        float minViewportHeight,
        float scrollIntoViewMargin = GumScrollListLayout.DefaultScrollIntoViewMargin,
        float listBottomPadding = 28f)
    {
        if (listScroll is null
            || listPanel is null
            || listFocusStartIndex < 0
            || focusIndex < listFocusStartIndex
            || focusIndex >= listFocusStartIndex + listFocusCount)
        {
            return;
        }

        GumScrollViewerChrome.StealFocusFromScrollChrome(listScroll);
        GumScrollListLayout.EnsureFocusedRowVisible(
            listScroll,
            listPanel,
            entries[focusIndex].Button.Visual,
            listFocusStartIndex,
            listFocusCount,
            focusIndex,
            stackSpacing,
            minViewportHeight,
            scrollIntoViewMargin,
            listBottomPadding);
    }
}
