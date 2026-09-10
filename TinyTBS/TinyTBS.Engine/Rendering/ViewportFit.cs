using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Engine.Rendering;

/// <summary>
/// Engine helper: fit a texture into a viewport while preserving aspect ratio.
/// </summary>
public static class ViewportFit
{
    public static Rectangle Centered(
        int contentWidth,
        int contentHeight,
        int viewportWidth,
        int viewportHeight)
    {
        if (contentWidth <= 0 || contentHeight <= 0)
            return Rectangle.Empty;

        var scale = Math.Min(
            (float)viewportWidth / contentWidth,
            (float)viewportHeight / contentHeight);

        var drawWidth = (int)(contentWidth * scale);
        var drawHeight = (int)(contentHeight * scale);

        return new Rectangle(
            (viewportWidth - drawWidth) / 2,
            (viewportHeight - drawHeight) / 2,
            drawWidth,
            drawHeight);
    }

    public static void DrawCentered(
        SpriteBatch spriteBatch,
        Texture2D texture,
        int viewportWidth,
        int viewportHeight,
        Color color)
    {
        var dest = Centered(texture.Width, texture.Height, viewportWidth, viewportHeight);
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        spriteBatch.Draw(texture, dest, color);
        spriteBatch.End();
    }
}
