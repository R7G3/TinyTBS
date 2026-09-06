using Microsoft.Xna.Framework;
using TinyTBS.Core.Input;
using TinyTBS.Core.Match;

namespace TinyTBS.Game.Rendering;

/// <summary>
/// Pixel layout for the match grid (centered in the viewport).
/// </summary>
public sealed class MatchBoardLayout
{
    public const int DefaultTileSizePixels = 64;

    public MatchBoardLayout(
        int width = MatchDefaults.GridWidth,
        int height = MatchDefaults.GridHeight,
        int tileSizePixels = DefaultTileSizePixels)
    {
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

    public Vector2 CellToWorldCenter(GridCell cell) =>
        Origin + new Vector2((cell.X + 0.5f) * TileSize, (cell.Y + 0.5f) * TileSize);

    public bool TryScreenToCell(ScreenPoint screenPosition, out GridCell cell) =>
        TryScreenToCell(new Vector2(screenPosition.X, screenPosition.Y), out cell);

    public bool TryScreenToCell(Vector2 screenPosition, out GridCell cell)
    {
        var local = screenPosition - Origin;
        if (local.X < 0 || local.Y < 0)
        {
            cell = default;
            return false;
        }

        var x = (int)(local.X / TileSize);
        var y = (int)(local.Y / TileSize);
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            cell = default;
            return false;
        }

        cell = new GridCell(x, y);
        return true;
    }
}
