using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Rules.Match;

namespace TinyTBS.Game.Presentation.Match.Board;

/// <summary>
/// Builds the match cursor layers from <see cref="CursorPalette"/> and asks
/// <see cref="CursorHighlightRenderer"/> to paint them.
/// </summary>
public static class MatchBoardHighlight
{
    private static readonly Func<GridCell, int> ReadX = static cell => cell.X;

    private static readonly Func<GridCell, int> ReadY = static cell => cell.Y;

    /// <summary>Reused for the duration of <see cref="Draw"/>; the renderer does not keep the layers.</summary>
    private static readonly CellHighlightLayer<GridCell>[] LayerBuffer = new CellHighlightLayer<GridCell>[5];

    public static void Draw(
        CursorHighlightRenderer renderer,
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        float cursorCellX,
        float cursorCellY,
        bool hasSelection,
        IReadOnlyList<GridCell>? moveRangeCells,
        IReadOnlyList<GridCell>? attackTargetCells,
        IReadOnlyList<GridCell>? raiseTargetCells,
        IReadOnlyList<GridCell>? captureTargetCells,
        IReadOnlyList<GridCell>? repairTargetCells)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        var moveOnly = ExceptActionCells(
            moveRangeCells,
            captureTargetCells,
            repairTargetCells,
            attackTargetCells,
            raiseTargetCells);

        var count = 0;
        Add(LayerBuffer, ref count, moveOnly, CursorPalette.Move);
        Add(LayerBuffer, ref count, captureTargetCells, CursorPalette.Capture);
        Add(LayerBuffer, ref count, repairTargetCells, CursorPalette.Repair);
        Add(LayerBuffer, ref count, attackTargetCells, CursorPalette.Attack);
        Add(LayerBuffer, ref count, raiseTargetCells, CursorPalette.Raise);

        renderer.Draw(
            spriteBatch,
            layout,
            cursorCellX,
            cursorCellY,
            hasSelection ? CursorPalette.Selected : CursorPalette.Idle,
            LayerBuffer.AsSpan(0, count),
            ReadX,
            ReadY);
    }

    private static void Add(
        CellHighlightLayer<GridCell>[] layers,
        ref int count,
        IReadOnlyList<GridCell>? cells,
        CellHighlightStyle style)
    {
        if (cells is not { Count: > 0 })
            return;

        layers[count++] = new CellHighlightLayer<GridCell>(cells, style);
    }

    private static IReadOnlyList<GridCell>? ExceptActionCells(
        IReadOnlyList<GridCell>? moveCells,
        params IReadOnlyList<GridCell>?[] actionSets)
    {
        if (moveCells is not { Count: > 0 })
            return moveCells;

        HashSet<GridCell>? excluded = null;
        foreach (var set in actionSets)
            AddAll(ref excluded, set);
        if (excluded is null)
            return moveCells;

        List<GridCell>? filtered = null;
        foreach (var cell in moveCells)
        {
            if (excluded.Contains(cell))
            {
                filtered ??= CopyUntil(moveCells, cell);
                continue;
            }

            filtered?.Add(cell);
        }

        return filtered ?? moveCells;
    }

    private static void AddAll(ref HashSet<GridCell>? set, IReadOnlyList<GridCell>? cells)
    {
        if (cells is not { Count: > 0 })
            return;

        set ??= new HashSet<GridCell>();
        foreach (var cell in cells)
            set.Add(cell);
    }

    private static List<GridCell> CopyUntil(IReadOnlyList<GridCell> source, GridCell stopExclusive)
    {
        var list = new List<GridCell>(source.Count);
        foreach (var cell in source)
        {
            if (cell == stopExclusive)
                break;
            list.Add(cell);
        }

        return list;
    }
}
