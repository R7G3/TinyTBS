using Gum.Forms.Controls;

namespace TinyTBS.Game.Presentation.Match.Controls;

internal static class GumMatchVisibility
{
    public static void SetVisible(FrameworkElement? element, bool visible)
    {
        if (element is null)
            return;

        // Forms hit-testing uses IsVisible; Visual.Visible alone leaves invisible overlays stealing clicks.
        element.IsVisible = visible;
    }
}
