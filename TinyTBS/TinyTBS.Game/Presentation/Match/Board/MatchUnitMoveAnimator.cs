using Microsoft.Xna.Framework;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Presentation.Match.Board;

/// <summary>
/// Smooth unit slide along a cell path. Logic already sits on the destination; this is presentation only.
/// Waypoints are refreshed each tick from cell→world so zoom/pan mid-walk stays correct.
/// </summary>
public sealed class MatchUnitMoveAnimator
{
    /// <summary>Seconds to cross one tile edge.</summary>
    public const float SecondsPerTile = 0.14f;

    private int _unitId = -1;
    private GridCell _targetCell;
    private GridCell[] _pathCells = [];
    private float _distanceAlongTiles;
    private float _totalTiles;
    private bool _active;

    public bool IsActive => _active;

    public int? UnitId => _active ? _unitId : null;

    public GridCell TargetCell => _targetCell;

    /// <summary>
    /// Starts a walk along <paramref name="pathCells"/> (must include start and end).
    /// </summary>
    public void Begin(int unitId, IReadOnlyList<GridCell> pathCells)
    {
        ArgumentNullException.ThrowIfNull(pathCells);

        if (pathCells.Count < 2)
        {
            Cancel();
            return;
        }

        _pathCells = new GridCell[pathCells.Count];
        for (var i = 0; i < pathCells.Count; i++)
            _pathCells[i] = pathCells[i];

        _totalTiles = _pathCells.Length - 1;
        _unitId = unitId;
        _targetCell = _pathCells[^1];
        _distanceAlongTiles = 0f;
        _active = true;
    }

    public void Cancel()
    {
        _active = false;
        _unitId = -1;
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

    public bool TryGetVisualTopLeft(int unitId, Func<int, int, Vector2> cellTopLeft, out Vector2 topLeft)
    {
        ArgumentNullException.ThrowIfNull(cellTopLeft);

        if (!_active || unitId != _unitId || _pathCells.Length < 2)
        {
            topLeft = default;
            return false;
        }

        var along = Math.Clamp(_distanceAlongTiles, 0f, _totalTiles);
        var segmentIndex = Math.Min((int)along, _pathCells.Length - 2);
        var t = along - segmentIndex;
        var from = _pathCells[segmentIndex];
        var to = _pathCells[segmentIndex + 1];
        topLeft = Vector2.Lerp(
            cellTopLeft(from.X, from.Y),
            cellTopLeft(to.X, to.Y),
            Math.Clamp(t, 0f, 1f));
        return true;
    }
}
