using TinyTBS.Game.Match;

namespace TinyTBS.Game.Presentation.Match.Board;

/// <summary>
/// Smooth board-cursor slide along an ortho (Manhattan) cell path.
/// Used so a bot turn looks like someone steering the cursor, not teleporting it.
/// </summary>
public sealed class MatchCursorAnimator
{
    /// <summary>Seconds to cross one tile — faster than unit walks.</summary>
    public const float SecondsPerTile = 0.07f;

    private GridCell[] _pathCells = [];
    private float _distanceAlongTiles;
    private float _totalTiles;
    private bool _active;

    public bool IsActive => _active;

    public GridCell TargetCell =>
        _pathCells.Length > 0 ? _pathCells[^1] : default;

    /// <summary>
    /// Starts aiming from <paramref name="from"/> to <paramref name="to"/>.
    /// Returns false if already on target (no animation).
    /// </summary>
    public bool Begin(GridCell from, GridCell to)
    {
        if (from == to)
        {
            Cancel();
            return false;
        }

        var path = BuildManhattanPath(from, to);
        if (path.Count < 2)
        {
            Cancel();
            return false;
        }

        _pathCells = new GridCell[path.Count];
        for (var i = 0; i < path.Count; i++)
            _pathCells[i] = path[i];

        _totalTiles = _pathCells.Length - 1;
        _distanceAlongTiles = 0f;
        _active = true;
        return true;
    }

    public void Cancel()
    {
        _active = false;
        _pathCells = [];
        _distanceAlongTiles = 0f;
        _totalTiles = 0f;
    }

    public void Update(float elapsedSeconds)
    {
        if (!_active)
            return;

        _distanceAlongTiles += Math.Max(0f, elapsedSeconds) / SecondsPerTile;
        if (_distanceAlongTiles >= _totalTiles)
            Cancel();
    }

    /// <summary>Fractional cell coordinates for drawing the cursor rectangle.</summary>
    public bool TryGetVisualCell(out float cellX, out float cellY)
    {
        if (!_active || _pathCells.Length < 2)
        {
            cellX = 0f;
            cellY = 0f;
            return false;
        }

        var along = Math.Clamp(_distanceAlongTiles, 0f, _totalTiles);
        var segmentIndex = Math.Min((int)along, _pathCells.Length - 2);
        var t = Math.Clamp(along - segmentIndex, 0f, 1f);
        var from = _pathCells[segmentIndex];
        var to = _pathCells[segmentIndex + 1];
        cellX = from.X + (to.X - from.X) * t;
        cellY = from.Y + (to.Y - from.Y) * t;
        return true;
    }

    /// <summary>Nearest cell under the sliding cursor (for syncing <see cref="MatchState.Cursor"/>).</summary>
    public bool TryGetLogicalCell(out GridCell cell)
    {
        if (!_active || _pathCells.Length < 2)
        {
            cell = default;
            return false;
        }

        if (!TryGetVisualCell(out var cellX, out var cellY))
        {
            cell = default;
            return false;
        }

        cell = new GridCell(
            (int)MathF.Round(cellX),
            (int)MathF.Round(cellY));
        return true;
    }

    /// <summary>Horizontal then vertical — same length as any Manhattan route.</summary>
    private static List<GridCell> BuildManhattanPath(GridCell from, GridCell to)
    {
        var path = new List<GridCell> { from };
        var x = from.X;
        var y = from.Y;
        while (x != to.X)
        {
            x += Math.Sign(to.X - x);
            path.Add(new GridCell(x, y));
        }

        while (y != to.Y)
        {
            y += Math.Sign(to.Y - y);
            path.Add(new GridCell(x, y));
        }

        return path;
    }
}
