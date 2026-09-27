using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Engine.Rendering;

/// <summary>
/// Cursor cell overlay plus optional move-range and attack-target tints on the match board.
/// Expects <see cref="MatchBoardLayout"/> already prepared for the viewport.
/// </summary>
public sealed class CursorHighlightRenderer : IDisposable
{
    private static readonly Color IdleBorder = new(255, 220, 80);
    private static readonly Color SelectedBorder = new(120, 255, 160);
    private static readonly Color MoveRangeFill = new(70, 140, 255, 70);
    private static readonly Color MoveRangeBorder = new(90, 170, 255, 160);
    private static readonly Color AttackTargetFill = new(220, 50, 50, 90);
    private static readonly Color AttackTargetBorder = new(255, 80, 80, 200);
    private static readonly Color RaiseTargetFill = new(160, 80, 220, 90);
    private static readonly Color RaiseTargetBorder = new(190, 120, 255, 200);

    private readonly Texture2D _pixel;

    public CursorHighlightRenderer(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    /// <param name="manageBatch">
    /// When true, opens/closes its own SpriteBatch. When false, draws into an already-begun batch
    /// (board pass shared with tiles and units).
    /// </param>
    public void Draw(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        int cursorX,
        int cursorY,
        bool hasSelection,
        IReadOnlyList<(int X, int Y)>? moveRangeCells = null,
        IReadOnlyList<(int X, int Y)>? attackTargetCells = null,
        IReadOnlyList<(int X, int Y)>? raiseTargetCells = null,
        bool manageBatch = true)
    {
        if (manageBatch)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);
        }

        DrawCellOverlays(spriteBatch, layout, moveRangeCells, MoveRangeFill, MoveRangeBorder, borderThickness: 1);
        DrawCellOverlays(spriteBatch, layout, attackTargetCells, AttackTargetFill, AttackTargetBorder, borderThickness: 2);
        DrawCellOverlays(spriteBatch, layout, raiseTargetCells, RaiseTargetFill, RaiseTargetBorder, borderThickness: 2);

        var topLeft = layout.Origin + new Vector2(cursorX * layout.TileSize, cursorY * layout.TileSize);
        var rect = new Rectangle((int)topLeft.X, (int)topLeft.Y, layout.TileSize, layout.TileSize);
        var borderColor = hasSelection ? SelectedBorder : IdleBorder;

        spriteBatch.Draw(_pixel, rect, Color.White * 0.18f);
        SpriteBatchPrimitives.DrawRectBorder(spriteBatch, _pixel, rect, borderColor, thickness: 2);

        if (manageBatch)
            spriteBatch.End();
    }

    private void DrawCellOverlays(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        IReadOnlyList<(int X, int Y)>? cells,
        Color fill,
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
            spriteBatch.Draw(_pixel, cellRect, fill);
            SpriteBatchPrimitives.DrawRectBorder(spriteBatch, _pixel, cellRect, border, borderThickness);
        }
    }

    public void Dispose() => _pixel.Dispose();
}
