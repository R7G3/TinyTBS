namespace TinyTBS.Core.Match;

/// <summary>
/// Pure match rules and state: select own unit, move one tile, end turn.
/// No viewport, textures, or draw systems.
/// </summary>
public sealed class MatchState
{
    private readonly List<MatchUnit> _units = [];
    private int _nextUnitId;

    private MatchState()
    {
    }

    public static MatchState CreateDemo()
    {
        var match = new MatchState
        {
            Cursor = new GridCell(MatchDefaults.GridWidth / 2, MatchDefaults.GridHeight / 2),
        };

        match.AddUnit(new GridCell(1, 1), playerIndex: 0);
        match.AddUnit(
            new GridCell(MatchDefaults.GridWidth - 2, MatchDefaults.GridHeight - 2),
            playerIndex: 1);
        return match;
    }

    public IReadOnlyList<MatchUnit> Units => _units;

    public int CurrentPlayer { get; private set; }

    public int? SelectedUnitId { get; private set; }

    public GridCell Cursor { get; private set; }

    public string StatusText
    {
        get
        {
            if (SelectedUnitId is int unitId && TryGetUnit(unitId, out var unit))
            {
                return $"Player {CurrentPlayer + 1} — unit P{unit.PlayerIndex + 1} at {unit.Cell}";
            }

            return $"Player {CurrentPlayer + 1} — select a unit";
        }
    }

    public void MoveCursor(int deltaX, int deltaY)
    {
        var x = Math.Clamp(Cursor.X + deltaX, 0, MatchDefaults.GridWidth - 1);
        var y = Math.Clamp(Cursor.Y + deltaY, 0, MatchDefaults.GridHeight - 1);
        Cursor = new GridCell(x, y);
    }

    public void HandleConfirm()
    {
        if (SelectedUnitId is null)
        {
            TrySelectUnitAt(Cursor);
            return;
        }

        TryMoveSelectedUnitTo(Cursor);
    }

    public void HandlePointer(GridCell cell) => Cursor = cell;

    public void EndTurn()
    {
        CurrentPlayer = (CurrentPlayer + 1) % MatchDefaults.PlayerCount;
        SelectedUnitId = null;
    }

    private MatchUnit AddUnit(GridCell cell, int playerIndex)
    {
        var unit = new MatchUnit(_nextUnitId++, cell, playerIndex);
        _units.Add(unit);
        return unit;
    }

    private bool TryGetUnit(int unitId, out MatchUnit unit)
    {
        foreach (var candidate in _units)
        {
            if (candidate.Id != unitId)
                continue;

            unit = candidate;
            return true;
        }

        unit = null!;
        return false;
    }

    private void TrySelectUnitAt(GridCell cell)
    {
        foreach (var unit in _units)
        {
            if (unit.Cell != cell)
                continue;

            if (unit.PlayerIndex != CurrentPlayer)
                return;

            SelectedUnitId = unit.Id;
            return;
        }
    }

    private void TryMoveSelectedUnitTo(GridCell destination)
    {
        if (SelectedUnitId is not int unitId || !TryGetUnit(unitId, out var unit))
            return;

        if (unit.Cell.ManhattanDistanceTo(destination) != 1)
            return;

        if (IsOccupied(destination, exceptUnitId: unitId))
            return;

        unit.Cell = destination;
        SelectedUnitId = null;
    }

    private bool IsOccupied(GridCell cell, int? exceptUnitId = null)
    {
        foreach (var unit in _units)
        {
            if (exceptUnitId == unit.Id)
                continue;

            if (unit.Cell == cell)
                return true;
        }

        return false;
    }
}
