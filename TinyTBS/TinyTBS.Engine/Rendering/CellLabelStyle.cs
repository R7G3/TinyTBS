using Microsoft.Xna.Framework;

namespace TinyTBS.Engine.Rendering;

/// <summary>Fill and outline for one cell label. The caller chooses the colors.</summary>
public readonly struct CellLabelStyle(Color fill, Color outline)
{
    public Color Fill { get; } = fill;

    public Color Outline { get; } = outline;
}
