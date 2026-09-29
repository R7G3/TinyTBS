using Gum.Forms.Controls;
using Gum.Wireframe;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Detects Gum text typing so Backspace (Cancel) does not leave screens.</summary>
public static class EditorGumTextEntry
{
    public static bool IsAnyFocused(params TextBox?[] boxes)
    {
        foreach (var box in boxes)
        {
            if (box is { IsFocused: true })
                return true;
        }

        return IsReceiverTextBox();
    }

    public static bool IsReceiverTextBox()
    {
        var receiver = InteractiveGue.CurrentInputReceiver;
        return receiver is TextBox or TextBoxBase;
    }
}
