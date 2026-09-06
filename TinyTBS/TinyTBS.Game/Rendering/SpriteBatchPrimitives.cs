using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Game.Rendering;

/// <summary>
/// Shared SpriteBatch primitives for filled rect borders and similar.
/// </summary>
public static class SpriteBatchPrimitives
{
    public static void DrawRectBorder(
        SpriteBatch batch,
        Texture2D pixel,
        Rectangle rect,
        Color color,
        int thickness)
    {
        batch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        batch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        batch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        batch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
