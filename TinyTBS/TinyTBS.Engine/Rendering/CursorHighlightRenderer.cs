using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Engine.Rendering;

/// <summary>
/// Paints a cursor cell and any number of cell layers. Fills use Multiply; borders use alpha.
/// Layer order and colors come from the caller. Expects <see cref="MatchBoardLayout"/> prepared for the viewport.
/// </summary>
public sealed class CursorHighlightRenderer : IDisposable
{
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
        float cursorCellX,
        float cursorCellY,
        CellHighlightStyle cursorStyle) =>
        Draw<int>(
            spriteBatch,
            layout,
            cursorCellX,
            cursorCellY,
            cursorStyle,
            ReadOnlySpan<CellHighlightLayer<int>>.Empty,
            static _ => 0,
            static _ => 0);

    public void Draw<TCell>(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        float cursorCellX,
        float cursorCellY,
        CellHighlightStyle cursorStyle,
        ReadOnlySpan<CellHighlightLayer<TCell>> layers,
        Func<TCell, int> cellX,
        Func<TCell, int> cellY)
    {
        ArgumentNullException.ThrowIfNull(cellX);
        ArgumentNullException.ThrowIfNull(cellY);

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            _multiplyBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        foreach (var layer in layers)
            DrawCellFills(spriteBatch, layout, layer.Cells, layer.Style.Fill, cellX, cellY);

        spriteBatch.End();

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        foreach (var layer in layers)
        {
            DrawCellBorders(
                spriteBatch,
                layout,
                layer.Cells,
                layer.Style.Border,
                layer.Style.BorderThickness,
                cellX,
                cellY);
        }

        DrawCursor(spriteBatch, layout, cursorCellX, cursorCellY, cursorStyle);

        spriteBatch.End();
    }

    private void DrawCursor(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        float cursorCellX,
        float cursorCellY,
        CellHighlightStyle cursorStyle)
    {
        var topLeft = layout.Origin
            + new Vector2(cursorCellX * layout.TileSize, cursorCellY * layout.TileSize);
        var rect = new Rectangle(
            (int)MathF.Round(topLeft.X),
            (int)MathF.Round(topLeft.Y),
            layout.TileSize,
            layout.TileSize);

        spriteBatch.Draw(_pixel, rect, cursorStyle.Fill);
        SpriteBatchPrimitives.DrawRectBorder(
            spriteBatch,
            _pixel,
            rect,
            cursorStyle.Border,
            thickness: cursorStyle.BorderThickness);
    }

    private void DrawCellFills<TCell>(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        IReadOnlyList<TCell>? cells,
        Color fill,
        Func<TCell, int> cellX,
        Func<TCell, int> cellY)
    {
        if (cells is not { Count: > 0 })
            return;

        foreach (var cell in cells)
        {
            var cellTopLeft = layout.Origin + new Vector2(cellX(cell) * layout.TileSize, cellY(cell) * layout.TileSize);
            var cellRect = new Rectangle(
                (int)cellTopLeft.X,
                (int)cellTopLeft.Y,
                layout.TileSize,
                layout.TileSize);
            spriteBatch.Draw(_pixel, cellRect, fill);
        }
    }

    private void DrawCellBorders<TCell>(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        IReadOnlyList<TCell>? cells,
        Color border,
        int borderThickness,
        Func<TCell, int> cellX,
        Func<TCell, int> cellY)
    {
        if (cells is not { Count: > 0 } || borderThickness <= 0)
            return;

        foreach (var cell in cells)
        {
            var cellTopLeft = layout.Origin + new Vector2(cellX(cell) * layout.TileSize, cellY(cell) * layout.TileSize);
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
