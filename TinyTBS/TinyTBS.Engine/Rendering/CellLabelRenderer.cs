using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Engine.Rendering;

/// <summary>
/// Draws one outlined string in a corner of a board cell. Inset and scale follow <see cref="MatchBoardLayout.TileSize"/>.
/// Expects an open <see cref="SpriteBatch"/>. Text, corner and <see cref="CellLabelStyle"/> come from the caller.
/// </summary>
public sealed class CellLabelRenderer
{
    private readonly SpriteFont _font;

    public CellLabelRenderer(SpriteFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        _font = font;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        Vector2 cellTopLeft,
        string text,
        CellLabelCorner corner,
        CellLabelStyle style)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentException.ThrowIfNullOrEmpty(text);

        var inset = Math.Max(2f, layout.TileSize * 0.06f);
        var scale = Math.Clamp(layout.TileSize / MatchBoardLayout.DefaultTileSizePixels, 0.55f, 1.35f);
        var size = _font.MeasureString(text) * scale;
        var bottom = cellTopLeft.Y + layout.TileSize - inset - size.Y;
        var position = corner == CellLabelCorner.BottomLeft
            ? new Vector2(cellTopLeft.X + inset, bottom)
            : new Vector2(cellTopLeft.X + layout.TileSize - inset - size.X, bottom);

        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                spriteBatch.DrawString(
                    _font,
                    text,
                    position + new Vector2(dx, dy),
                    style.Outline,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    layerDepth: 0f);
            }
        }

        spriteBatch.DrawString(
            _font,
            text,
            position,
            style.Fill,
            rotation: 0f,
            origin: Vector2.Zero,
            scale,
            SpriteEffects.None,
            layerDepth: 0f);
    }
}
