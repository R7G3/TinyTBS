using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Core.Match;

namespace TinyTBS.Game.Rendering;

/// <summary>
/// Engine: cursor cell overlay on the match board.
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

    public void Draw(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        GridCell cursor,
        bool hasSelection)
    {
        var topLeft = layout.Origin + new Vector2(cursor.X * layout.TileSize, cursor.Y * layout.TileSize);
        var rect = new Rectangle((int)topLeft.X, (int)topLeft.Y, layout.TileSize, layout.TileSize);
        var borderColor = hasSelection ? SelectedBorder : IdleBorder;

        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, rect, Color.White * 0.18f);
        SpriteBatchPrimitives.DrawRectBorder(spriteBatch, _pixel, rect, borderColor, thickness: 2);
        spriteBatch.End();
    }

    public void Dispose() => _pixel.Dispose();
}
