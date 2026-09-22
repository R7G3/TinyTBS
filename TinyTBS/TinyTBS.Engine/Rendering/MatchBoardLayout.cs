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

    public const float MinZoom = 0.5f;

    public const float MaxZoom = 2.5f;

    /// <summary>Additive zoom change per mouse-wheel notch.</summary>
    public const float WheelZoomStep = 0.1f;

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
        BaseTileSize = tileSizePixels;
        Zoom = 1f;
    }

    public Vector2 Origin { get; private set; }

    /// <summary>Unscaled tile size in pixels (content art size).</summary>
    public int BaseTileSize { get; }

    /// <summary>Current board zoom (1 = default). Clamped to <see cref="MinZoom"/>..<see cref="MaxZoom"/>.</summary>
    public float Zoom { get; private set; }

    /// <summary>Effective cell size after zoom (used for hit-tests and drawing).</summary>
    public int TileSize => Math.Max(8, (int)MathF.Round(BaseTileSize * Zoom));

    public int Width { get; }

    public int Height { get; }

    public void UpdateForViewport(int viewportWidth, int viewportHeight)
    {
        var tileSize = TileSize;
        var gridWidth = Width * tileSize;
        var gridHeight = Height * tileSize;
        Origin = new Vector2(
            (viewportWidth - gridWidth) * 0.5f,
            (viewportHeight - gridHeight) * 0.5f);
    }

    /// <summary>Adds to zoom (positive = zoom in). Clamped.</summary>
    public void AdjustZoom(float delta)
    {
        if (delta == 0f)
            return;

        Zoom = Math.Clamp(Zoom + delta, MinZoom, MaxZoom);
    }

    /// <summary>Applies discrete zoom steps (positive = zoom in).</summary>
    public void ZoomBySteps(int steps)
    {
        if (steps == 0)
            return;

        AdjustZoom(steps * WheelZoomStep);
    }

    public bool TryScreenToCell(ScreenPoint screenPosition, out int cellX, out int cellY) =>
        TryScreenToCell(new Vector2(screenPosition.X, screenPosition.Y), out cellX, out cellY);

    public bool TryScreenToCell(Vector2 screenPosition, out int cellX, out int cellY)
    {
        var tileSize = TileSize;
        var local = screenPosition - Origin;
        if (local.X < 0 || local.Y < 0)
        {
            cellX = 0;
            cellY = 0;
            return false;
        }

        cellX = (int)(local.X / tileSize);
        cellY = (int)(local.Y / tileSize);
        if (cellX < 0 || cellY < 0 || cellX >= Width || cellY >= Height)
        {
            cellX = 0;
            cellY = 0;
            return false;
        }

        return true;
    }

    /// <summary>Top-center of a cell in screen pixels (for anchoring UI above the tile).</summary>
    public Vector2 GetCellTopCenter(int cellX, int cellY)
    {
        var tileSize = TileSize;
        return Origin + new Vector2((cellX + 0.5f) * tileSize, cellY * tileSize);
    }
}
