using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Engine.Rendering;

/// <summary>
/// Cursor cell overlay on the match board.
/// Expects <see cref="MatchBoardLayout"/> already prepared for the viewport.
/// </summary>
public sealed class CursorHighlightRenderer : IDisposable
{
    private static readonly Color IdleBorder = new(255, 220, 80);
    private static readonly Color SelectedBorder = new(120, 255, 160);

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
        bool manageBatch = true)
    {
        var topLeft = layout.Origin + new Vector2(cursorX * layout.TileSize, cursorY * layout.TileSize);
        var rect = new Rectangle((int)topLeft.X, (int)topLeft.Y, layout.TileSize, layout.TileSize);
        var borderColor = hasSelection ? SelectedBorder : IdleBorder;

        if (manageBatch)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);
        }

        spriteBatch.Draw(_pixel, rect, Color.White * 0.18f);
        SpriteBatchPrimitives.DrawRectBorder(spriteBatch, _pixel, rect, borderColor, thickness: 2);

        if (manageBatch)
            spriteBatch.End();
    }

    public void Dispose() => _pixel.Dispose();
}
