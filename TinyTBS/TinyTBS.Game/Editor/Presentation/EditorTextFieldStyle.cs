using TinyTBS.Game.Presentation.Shared;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Microsoft.Xna.Framework;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Shared TextBox look so fields read as editable, not disabled.</summary>
public static class EditorTextFieldStyle
{
    public static void Apply(TextBox textBox)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        if (textBox.Visual is not TextBoxVisual visual)
            return;

        visual.BackgroundColor = UiColors.EditorTextField;
        visual.ForegroundColor = UiColors.EditorTextFieldForeground;
        visual.PlaceholderColor = new Color(110, 118, 130);
    }
}
