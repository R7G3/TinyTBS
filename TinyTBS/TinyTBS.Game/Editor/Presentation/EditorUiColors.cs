using Microsoft.Xna.Framework;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Colors for Editor hub panels.</summary>
public static class EditorUiColors
{
    public static readonly Color Panel = UiPanelColors.Panel;

    public static readonly Color ListWell = new(12, 14, 22, 200);

    /// <summary>Editable TextBox fill — white so fields read clearly as inputs.</summary>
    public static readonly Color TextField = Color.White;

    /// <summary>Text on <see cref="TextField"/>.</summary>
    public static readonly Color TextFieldForeground = new(20, 24, 32);
}
