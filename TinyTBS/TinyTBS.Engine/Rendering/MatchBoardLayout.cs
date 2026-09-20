using Microsoft.Xna.Framework;
using TinyTBS.Engine.Input;

namespace TinyTBS.Engine.Rendering;

/// <summary>
/// Pixel layout for a rectangular tile grid (centered in the viewport).
/// Domain cell types stay in Game; this API uses integer coordinates only.
/// </summary>
public sealed class MatchBoardLayout
{
    public const int DefaultTileSizePixels = 64;

    public MatchBoardLayout(int width, int height, int tileSizePixels = DefaultTileSizePixels)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (tileSizePixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(tileSizePixels));

        Width = width;
        Height = height;
        TileSize = tileSizePixels;
    }

    public Vector2 Origin { get; private set; }

    public int TileSize { get; }

    public int Width { get; }

    public int Height { get; }

    public void UpdateForViewport(int viewportWidth, int viewportHeight)
    {
        var gridWidth = Width * TileSize;
        var gridHeight = Height * TileSize;
        Origin = new Vector2(
            (viewportWidth - gridWidth) * 0.5f,
            (viewportHeight - gridHeight) * 0.5f);
    }

    public bool TryScreenToCell(ScreenPoint screenPosition, out int cellX, out int cellY) =>
        TryScreenToCell(new Vector2(screenPosition.X, screenPosition.Y), out cellX, out cellY);

    public bool TryScreenToCell(Vector2 screenPosition, out int cellX, out int cellY)
    {
        var local = screenPosition - Origin;
        if (local.X < 0 || local.Y < 0)
        {
            cellX = 0;
            cellY = 0;
            return false;
        }

        cellX = (int)(local.X / TileSize);
        cellY = (int)(local.Y / TileSize);
        if (cellX < 0 || cellY < 0 || cellX >= Width || cellY >= Height)
        {
            cellX = 0;
            cellY = 0;
            return false;
        }

        return true;
    }
}
