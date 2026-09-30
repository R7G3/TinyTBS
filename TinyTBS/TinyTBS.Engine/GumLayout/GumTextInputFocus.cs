using Gum.Forms.Controls;
using Gum.Wireframe;

namespace TinyTBS.Engine.GumLayout;

/// <summary>Detects a Gum text box that currently receives keyboard input.</summary>
public static class GumTextInputFocus
{
    public static bool IsTextBoxReceivingInput() =>
        InteractiveGue.CurrentInputReceiver is TextBox or TextBoxBase;
}
