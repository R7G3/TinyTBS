using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Engine.Rendering;

/// <summary>
/// Cursor cell overlay plus move / attack / capture / repair / raise tints (Multiply fills).
/// Expects <see cref="MatchBoardLayout"/> already prepared for the viewport.
/// </summary>
public sealed class CursorHighlightRenderer : IDisposable
{
    private static readonly Color IdleBorder = new(255, 220, 80);
    private static readonly Color SelectedBorder = new(120, 255, 160);

    private static readonly Color MoveMultiply = new(150, 175, 230);
    private static readonly Color AttackMultiply = new(255, 140, 140);
    private static readonly Color CaptureMultiply = new(70, 255, 110);
    private static readonly Color RepairMultiply = new(255, 200, 70);
    private static readonly Color RaiseMultiply = new(210, 150, 255);

    private static readonly Color MoveBorder = new(90, 150, 230, 180);
    private static readonly Color AttackBorder = new(255, 80, 80, 210);
    private static readonly Color CaptureBorder = new(40, 255, 100, 255);
    private static readonly Color RepairBorder = new(255, 210, 60, 255);
    private static readonly Color RaiseBorder = new(200, 120, 255, 210);

    private const int CursorBorderThickness = 4;

    private readonly Texture2D _pixel;
    private readonly BlendState _multiplyBlend;

    public CursorHighlightRenderer(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _multiplyBlend = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.Zero,
            AlphaBlendFunction = BlendFunction.Add,
        };
    }

    public void Draw(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        int cursorX,
        int cursorY,
        bool hasSelection,
        IReadOnlyList<(int X, int Y)>? moveRangeCells = null,
        IReadOnlyList<(int X, int Y)>? attackTargetCells = null,
        IReadOnlyList<(int X, int Y)>? raiseTargetCells = null,
        IReadOnlyList<(int X, int Y)>? captureTargetCells = null,
        IReadOnlyList<(int X, int Y)>? repairTargetCells = null)
    {
        var moveOnly = ExceptActionCells(
            moveRangeCells,
            captureTargetCells,
            repairTargetCells,
            attackTargetCells,
            raiseTargetCells);

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            _multiplyBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        DrawCellFills(spriteBatch, layout, moveOnly, MoveMultiply);
        DrawCellFills(spriteBatch, layout, captureTargetCells, CaptureMultiply);
        DrawCellFills(spriteBatch, layout, repairTargetCells, RepairMultiply);
        DrawCellFills(spriteBatch, layout, attackTargetCells, AttackMultiply);
        DrawCellFills(spriteBatch, layout, raiseTargetCells, RaiseMultiply);

        spriteBatch.End();

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        DrawCellBorders(spriteBatch, layout, moveOnly, MoveBorder, borderThickness: 1);
        DrawCellBorders(spriteBatch, layout, captureTargetCells, CaptureBorder, borderThickness: 2);
        DrawCellBorders(spriteBatch, layout, repairTargetCells, RepairBorder, borderThickness: 2);
        DrawCellBorders(spriteBatch, layout, attackTargetCells, AttackBorder, borderThickness: 2);
        DrawCellBorders(spriteBatch, layout, raiseTargetCells, RaiseBorder, borderThickness: 2);
        DrawCursor(spriteBatch, layout, cursorX, cursorY, hasSelection);

        spriteBatch.End();
    }

    private void DrawCursor(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        int cursorX,
        int cursorY,
        bool hasSelection)
    {
        var topLeft = layout.Origin + new Vector2(cursorX * layout.TileSize, cursorY * layout.TileSize);
        var rect = new Rectangle((int)topLeft.X, (int)topLeft.Y, layout.TileSize, layout.TileSize);
        var borderColor = hasSelection ? SelectedBorder : IdleBorder;

        spriteBatch.Draw(_pixel, rect, Color.White * 0.18f);
        SpriteBatchPrimitives.DrawRectBorder(spriteBatch, _pixel, rect, borderColor, thickness: CursorBorderThickness);
    }

    private static IReadOnlyList<(int X, int Y)>? ExceptActionCells(
        IReadOnlyList<(int X, int Y)>? moveCells,
        params IReadOnlyList<(int X, int Y)>?[] actionSets)
    {
        if (moveCells is not { Count: > 0 })
            return moveCells;

        HashSet<(int X, int Y)>? excluded = null;
        foreach (var set in actionSets)
            AddAll(ref excluded, set);
        if (excluded is null)
            return moveCells;

        List<(int X, int Y)>? filtered = null;
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

    private static void AddAll(ref HashSet<(int X, int Y)>? set, IReadOnlyList<(int X, int Y)>? cells)
    {
        if (cells is not { Count: > 0 })
            return;

        set ??= new HashSet<(int X, int Y)>();
        foreach (var cell in cells)
            set.Add(cell);
    }

    private static List<(int X, int Y)> CopyUntil(
        IReadOnlyList<(int X, int Y)> source,
        (int X, int Y) stopExclusive)
    {
        var list = new List<(int X, int Y)>(source.Count);
        foreach (var cell in source)
        {
            if (cell == stopExclusive)
                break;
            list.Add(cell);
        }

        return list;
    }

    private void DrawCellFills(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        IReadOnlyList<(int X, int Y)>? cells,
        Color fill)
    {
        if (cells is not { Count: > 0 })
            return;

        foreach (var (x, y) in cells)
        {
            var cellTopLeft = layout.Origin + new Vector2(x * layout.TileSize, y * layout.TileSize);
            var cellRect = new Rectangle(
                (int)cellTopLeft.X,
                (int)cellTopLeft.Y,
                layout.TileSize,
                layout.TileSize);
            spriteBatch.Draw(_pixel, cellRect, fill);
        }
    }

    private void DrawCellBorders(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        IReadOnlyList<(int X, int Y)>? cells,
        Color border,
        int borderThickness)
    {
        if (cells is not { Count: > 0 })
            return;

        foreach (var (x, y) in cells)
        {
            var cellTopLeft = layout.Origin + new Vector2(x * layout.TileSize, y * layout.TileSize);
            var cellRect = new Rectangle(
                (int)cellTopLeft.X,
                (int)cellTopLeft.Y,
                layout.TileSize,
                layout.TileSize);
            SpriteBatchPrimitives.DrawRectBorder(spriteBatch, _pixel, cellRect, border, borderThickness);
        }
    }

    public void Dispose()
    {
        _pixel.Dispose();
        _multiplyBlend.Dispose();
    }
}
