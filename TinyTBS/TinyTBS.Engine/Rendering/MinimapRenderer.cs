using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Engine.Rendering;

/// <summary>
/// Match minimap: terrain baked into one texture; buildings/units drawn as few overlays.
/// </summary>
public sealed class MinimapRenderer : IDisposable
{
    private const int CircleTextureSize = 32;
    private static readonly Color MarkerOutline = Color.White;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Texture2D _pixel;
    private readonly Texture2D _circle;

    private Texture2D? _terrainTexture;
    private Color[]? _terrainPixels;
    private int _cachedWidth;
    private int _cachedHeight;
    private bool _terrainDirty = true;

    public MinimapRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _circle = CreateFilledCircleTexture(graphicsDevice, CircleTextureSize);
    }

    /// <summary>Call when map terrain cells change so the next draw rebuilds the cache.</summary>
    public void InvalidateTerrain() => _terrainDirty = true;

    public void Draw(
        SpriteBatch spriteBatch,
        Rectangle destination,
        int width,
        int height,
        Func<int, int, Color> terrainColor,
        IEnumerable<(int X, int Y, Color Color)> buildings,
        IEnumerable<(int X, int Y, Color Color)> units)
    {
        if (width <= 0 || height <= 0)
            return;

        EnsureTerrainCache(width, height, terrainColor);

        var cellWidth = destination.Width / (float)width;
        var cellHeight = destination.Height / (float)height;

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        spriteBatch.Draw(_pixel, destination, Color.Black * 0.65f);
        spriteBatch.Draw(_terrainTexture, destination, Color.White);

        foreach (var building in buildings)
        {
            var cell = CellRect(destination, building.X, building.Y, cellWidth, cellHeight);
            var square = BuildingMarkerSquare(cell, cellWidth, cellHeight);
            spriteBatch.Draw(_pixel, square, building.Color);
            var borderThickness = Math.Max(1, (int)(Math.Min(cellWidth, cellHeight) * 0.08f));
            SpriteBatchPrimitives.DrawRectBorder(
                spriteBatch,
                _pixel,
                square,
                MarkerOutline,
                borderThickness);
        }

        foreach (var unit in units)
        {
            var cell = CellRect(destination, unit.X, unit.Y, cellWidth, cellHeight);
            var square = BuildingMarkerSquare(cell, cellWidth, cellHeight);
            var fitSize = Math.Min(square.Width, square.Height);
            var outlineThickness = Math.Max(1, fitSize / 8);
            var outerDiameter = Math.Max(3, (int)(fitSize * 0.58f));
            outerDiameter = Math.Min(outerDiameter, fitSize);
            var innerDiameter = Math.Max(2, outerDiameter - outlineThickness * 2);

            var outer = CenteredSquare(square, outerDiameter);
            var inner = CenteredSquare(square, innerDiameter);
            spriteBatch.Draw(_circle, outer, MarkerOutline);
            spriteBatch.Draw(_circle, inner, unit.Color);
        }

        SpriteBatchPrimitives.DrawRectBorder(spriteBatch, _pixel, destination, Color.White * 0.8f, thickness: 2);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _terrainTexture?.Dispose();
        _terrainTexture = null;
        _circle.Dispose();
        _pixel.Dispose();
    }

    private void EnsureTerrainCache(int width, int height, Func<int, int, Color> terrainColor)
    {
        if (_terrainTexture is null || width != _cachedWidth || height != _cachedHeight)
        {
            _terrainTexture?.Dispose();
            _terrainTexture = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
            _cachedWidth = width;
            _cachedHeight = height;
            _terrainPixels = new Color[width * height];
            _terrainDirty = true;
        }

        if (!_terrainDirty || _terrainPixels is null || _terrainTexture is null)
            return;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                _terrainPixels[y * width + x] = terrainColor(x, y);
        }

        _terrainTexture.SetData(_terrainPixels);
        _terrainDirty = false;
    }

    private static Rectangle CellRect(
        Rectangle destination,
        int x,
        int y,
        float cellWidth,
        float cellHeight)
    {
        var left = destination.X + (int)(x * cellWidth);
        var top = destination.Y + (int)(y * cellHeight);
        var right = destination.X + (int)((x + 1) * cellWidth);
        var bottom = destination.Y + (int)((y + 1) * cellHeight);
        return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }

    /// <summary>Same inset square as building markers — units center inside it.</summary>
    private static Rectangle BuildingMarkerSquare(Rectangle cell, float cellWidth, float cellHeight)
    {
        var inset = Math.Max(1, (int)(Math.Min(cellWidth, cellHeight) * 0.15f));
        return new Rectangle(
            cell.X + inset,
            cell.Y + inset,
            Math.Max(1, cell.Width - inset * 2),
            Math.Max(1, cell.Height - inset * 2));
    }

    private static Rectangle CenteredSquare(Rectangle bounds, int size)
    {
        size = Math.Max(1, size);
        return new Rectangle(
            bounds.X + (bounds.Width - size) / 2,
            bounds.Y + (bounds.Height - size) / 2,
            size,
            size);
    }

    private static Texture2D CreateFilledCircleTexture(GraphicsDevice graphicsDevice, int size)
    {
        var texture = new Texture2D(graphicsDevice, size, size, false, SurfaceFormat.Color);
        var pixels = new Color[size * size];
        var center = (size - 1) * 0.5f;
        var radius = size * 0.5f - 0.35f;
        var radiusSquared = radius * radius;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var deltaX = x - center;
                var deltaY = y - center;
                pixels[y * size + x] = deltaX * deltaX + deltaY * deltaY <= radiusSquared
                    ? Color.White
                    : Color.Transparent;
            }
        }

        texture.SetData(pixels);
        return texture;
    }
}
