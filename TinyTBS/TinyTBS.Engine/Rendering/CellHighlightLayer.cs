namespace TinyTBS.Engine.Rendering;

/// <summary>One set of cells painted with the same <see cref="CellHighlightStyle"/>.</summary>
public readonly struct CellHighlightLayer<TCell>(IReadOnlyList<TCell>? cells, CellHighlightStyle style)
{
    public IReadOnlyList<TCell>? Cells { get; } = cells;

    public CellHighlightStyle Style { get; } = style;
}
