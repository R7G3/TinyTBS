using Microsoft.Xna.Framework;
using TinyTBS.Engine.Input;

namespace TinyTBS.Engine.Rendering;

/// <summary>
/// Pixel layout for a rectangular tile grid (centered in the viewport, with pan offset).
/// Domain cell types stay in Game; this API uses integer coordinates only.
/// </summary>
public sealed class MatchBoardLayout
{
    public const int DefaultTileSizePixels = 64;

    public const float MinZoom = 0.5f;

    public const float MaxZoom = 2.5f;

    /// <summary>Additive zoom change per mouse-wheel notch.</summary>
    public const float WheelZoomStep = 0.1f;

    /// <summary>
    /// GDD central camera zone: half the viewport, with quarter-screen margins on each side.
    /// </summary>
    public const float CentralZoneScreenFraction = 0.5f;

    /// <summary>
    /// Extra empty margin (in tiles at current zoom) allowed beyond the map when panning,
    /// so edge cells can clear HUD chrome.
    /// </summary>
    public const float PanOverscrollTiles = 1f;

    private int _viewportWidth;
    private int _viewportHeight;

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

    /// <summary>Pan offset in screen pixels, added to the centered origin.</summary>
    public Vector2 CameraOffset { get; private set; }

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
        _viewportWidth = Math.Max(1, viewportWidth);
        _viewportHeight = Math.Max(1, viewportHeight);
        ClampCameraOffset();
        RefreshOrigin();
    }

    /// <summary>Adds to zoom (positive = zoom in). Clamped.</summary>
    public void AdjustZoom(float delta)
    {
        if (delta == 0f)
            return;

        Zoom = Math.Clamp(Zoom + delta, MinZoom, MaxZoom);
        ClampCameraOffset();
        RefreshOrigin();
    }

    /// <summary>Applies discrete zoom steps (positive = zoom in).</summary>
    public void ZoomBySteps(int steps)
    {
        if (steps == 0)
            return;

        AdjustZoom(steps * WheelZoomStep);
    }

    /// <summary>Pans the board in screen pixels (positive X moves the map right on screen).</summary>
    public void PanBy(Vector2 delta)
    {
        if (delta == Vector2.Zero)
            return;

        CameraOffset += delta;
        ClampCameraOffset();
        RefreshOrigin();
    }

    /// <summary>
    /// If the cell center is outside the central zone (half the screen), pans so it sits on the nearest zone edge.
    /// </summary>
    public void KeepCellInCentralZone(int cellX, int cellY)
    {
        if (_viewportWidth <= 0 || _viewportHeight <= 0)
            return;

        var cellCenter = GetCellCenter(cellX, cellY);
        var marginX = _viewportWidth * (1f - CentralZoneScreenFraction) * 0.5f;
        var marginY = _viewportHeight * (1f - CentralZoneScreenFraction) * 0.5f;
        var zoneLeft = marginX;
        var zoneRight = _viewportWidth - marginX;
        var zoneTop = marginY;
        var zoneBottom = _viewportHeight - marginY;

        var pan = Vector2.Zero;
        if (cellCenter.X < zoneLeft)
            pan.X = zoneLeft - cellCenter.X;
        else if (cellCenter.X > zoneRight)
            pan.X = zoneRight - cellCenter.X;

        if (cellCenter.Y < zoneTop)
            pan.Y = zoneTop - cellCenter.Y;
        else if (cellCenter.Y > zoneBottom)
            pan.Y = zoneBottom - cellCenter.Y;

        if (pan != Vector2.Zero)
            PanBy(pan);
    }

    /// <summary>
    /// Clamps cell indices so the cell center stays inside the viewport (and on the map).
    /// Used while the player pans the camera so the cursor does not leave the screen.
    /// </summary>
    public void ClampCellToViewport(ref int cellX, ref int cellY)
    {
        if (_viewportWidth <= 0 || _viewportHeight <= 0)
            return;

        cellX = Math.Clamp(cellX, 0, Width - 1);
        cellY = Math.Clamp(cellY, 0, Height - 1);

        var tileSize = TileSize;
        if (!TryGetViewportCellRange(tileSize, out var minX, out var maxX, out var minY, out var maxY))
        {
            // No cell center fits in the viewport (heavy overscroll) — snap to nearest on-map cell.
            SnapCellToNearestVisible(tileSize, ref cellX, ref cellY);
            return;
        }

        cellX = Math.Clamp(cellX, minX, maxX);
        cellY = Math.Clamp(cellY, minY, maxY);
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

    /// <summary>Center of a cell in screen pixels.</summary>
    public Vector2 GetCellCenter(int cellX, int cellY)
    {
        var tileSize = TileSize;
        return Origin + new Vector2((cellX + 0.5f) * tileSize, (cellY + 0.5f) * tileSize);
    }

    private bool TryGetViewportCellRange(
        int tileSize,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        // Cell center on screen: 0 <= Origin + (cell + 0.5) * tileSize <= viewport.
        minX = (int)MathF.Ceiling(-Origin.X / tileSize - 0.5f);
        maxX = (int)MathF.Floor((_viewportWidth - Origin.X) / tileSize - 0.5f);
        minY = (int)MathF.Ceiling(-Origin.Y / tileSize - 0.5f);
        maxY = (int)MathF.Floor((_viewportHeight - Origin.Y) / tileSize - 0.5f);

        minX = Math.Clamp(minX, 0, Width - 1);
        maxX = Math.Clamp(maxX, 0, Width - 1);
        minY = Math.Clamp(minY, 0, Height - 1);
        maxY = Math.Clamp(maxY, 0, Height - 1);

        return minX <= maxX && minY <= maxY;
    }

    private void SnapCellToNearestVisible(int tileSize, ref int cellX, ref int cellY)
    {
        var local = new Vector2(_viewportWidth * 0.5f, _viewportHeight * 0.5f) - Origin;
        cellX = Math.Clamp((int)MathF.Floor(local.X / tileSize), 0, Width - 1);
        cellY = Math.Clamp((int)MathF.Floor(local.Y / tileSize), 0, Height - 1);
    }

    private void RefreshOrigin()
    {
        var centered = GetCenteredOrigin();
        Origin = centered + CameraOffset;
    }

    private Vector2 GetCenteredOrigin()
    {
        var tileSize = TileSize;
        var gridWidth = Width * tileSize;
        var gridHeight = Height * tileSize;
        return new Vector2(
            (_viewportWidth - gridWidth) * 0.5f,
            (_viewportHeight - gridHeight) * 0.5f);
    }

    private void ClampCameraOffset()
    {
        if (_viewportWidth <= 0 || _viewportHeight <= 0)
        {
            CameraOffset = Vector2.Zero;
            return;
        }

        var centered = GetCenteredOrigin();
        var tileSize = TileSize;
        var overscroll = tileSize * PanOverscrollTiles;
        var gridWidth = Width * tileSize;
        var gridHeight = Height * tileSize;

        CameraOffset = new Vector2(
            ClampAxis(CameraOffset.X, centered.X, _viewportWidth, gridWidth, overscroll),
            ClampAxis(CameraOffset.Y, centered.Y, _viewportHeight, gridHeight, overscroll));
    }

    private static float ClampAxis(
        float offset,
        float centeredOrigin,
        int viewportSize,
        int gridSize,
        float overscroll)
    {
        var minOrigin = viewportSize - gridSize - overscroll;
        var maxOrigin = overscroll;
        float minOffset;
        float maxOffset;
        if (minOrigin <= maxOrigin)
        {
            minOffset = minOrigin - centeredOrigin;
            maxOffset = maxOrigin - centeredOrigin;
        }
        else
        {
            // Map smaller than viewport: still allow ±1 tile from center to clear HUD.
            minOffset = -overscroll;
            maxOffset = overscroll;
        }

        return Math.Clamp(offset, minOffset, maxOffset);
    }
}