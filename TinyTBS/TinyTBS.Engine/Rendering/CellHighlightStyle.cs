using Microsoft.Xna.Framework;

namespace TinyTBS.Engine.Rendering;

/// <summary>Fill and border for one highlighted cell or layer. The caller chooses the colors.</summary>
public readonly struct CellHighlightStyle(Color fill, Color border, int borderThickness)
{
    public Color Fill { get; } = fill;

    public Color Border { get; } = border;

    public int BorderThickness { get; } = borderThickness;
}
