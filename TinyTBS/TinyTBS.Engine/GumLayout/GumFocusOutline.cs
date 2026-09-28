using Gum.Forms;
using Gum.Forms.Controls;

namespace TinyTBS.Engine.GumLayout;

/// <summary>
/// Replaces Gum V3's default bottom focus bar with a full outline around the control.
/// </summary>
public static class GumFocusOutline
{
    /// <summary>Outline stroke thickness in screen pixels.</summary>
    public const float ThicknessPixels = 3f;

    /// <summary>
    /// Registers default Forms templates so every new <see cref="Button"/> uses outline focus.
    /// Call after GumService initialization.
    /// </summary>
    public static void InstallDefaultButtonTemplate()
    {
        FrameworkElement.DefaultFormsTemplates[typeof(Button)] = new VisualTemplate(() =>
            new OutlineFocusButtonVisual());
    }
}
