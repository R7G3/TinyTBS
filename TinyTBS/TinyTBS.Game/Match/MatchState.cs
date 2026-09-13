namespace TinyTBS.Game.Match;

/// <summary>
/// Pure match rules and state: select own unit, move one tile, end turn.
/// No viewport, textures, or draw systems.
/// </summary>
public sealed class MatchState
{
    private readonly TerrainKind[,] _terrain;
    private readonly List<MatchBuilding> _buildings = [];
    private readonly List<MatchUnit> _units = [];
    private int _nextUnitId;

    private MatchState(int width, int height)
    {
        Width = width;
        Height = height;
        _terrain = new TerrainKind[width, height];
    }

    public static MatchState CreateDemo()
    {
        var match = new MatchState(MatchDefaults.GridWidth, MatchDefaults.GridHeight)
        {
            Cursor = new GridCell(2, 2),
        };

        for (var y = 0; y < match.Height; y++)
        {
            for (var x = 0; x < match.Width; x++)
                match._terrain[x, y] = TerrainKind.Grass;
        }

        // Water column with a bridge crossing.
        for (var y = 1; y <= 5; y++)
            match._terrain[3, y] = TerrainKind.Water;
        match._terrain[3, 3] = TerrainKind.Bridge;

        // Road strip.
        for (var i = 0; i < match.Width; i++)
        {
            if (match._terrain[i, i] == TerrainKind.Grass)
                match._terrain[i, i] = TerrainKind.Road;
        }

        match._terrain[6, 1] = TerrainKind.Mountain;
        match._terrain[6, 2] = TerrainKind.Mountain;
        match._terrain[5, 1] = TerrainKind.Mountain;

        match._buildings.Add(new MatchBuilding(BuildingKind.Castle, new GridCell(1, 1), ownerPlayerIndex: 0));
        match._buildings.Add(new MatchBuilding(BuildingKind.Village, new GridCell(6, 6), ownerPlayerIndex: 1));
        match._buildings.Add(new MatchBuilding(BuildingKind.Village, new GridCell(4, 1), ownerPlayerIndex: null));

        match.AddUnit(UnitKind.King, new GridCell(1, 2), playerIndex: 0);
        match.AddUnit(UnitKind.Swordsman, new GridCell(2, 1), playerIndex: 0);
        match.AddUnit(UnitKind.King, new GridCell(6, 5), playerIndex: 1);
        match.AddUnit(UnitKind.Swordsman, new GridCell(5, 6), playerIndex: 1);

        return match;
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<MatchBuilding> Buildings => _buildings;

    public IReadOnlyList<MatchUnit> Units => _units;

    public int CurrentPlayer { get; private set; }

    public int? SelectedUnitId { get; private set; }

    public GridCell Cursor { get; private set; }

    public TerrainKind GetTerrain(GridCell cell) => _terrain[cell.X, cell.Y];

    public TerrainKind GetTerrain(int x, int y) => _terrain[x, y];

    public string StatusText
    {
        get
        {
            if (SelectedUnitId is int unitId && TryGetUnit(unitId, out var unit))
            {
                return $"P{CurrentPlayer + 1} — {unit.Kind} at {unit.Cell} ({GetTerrain(unit.Cell)})";
            }

            return $"P{CurrentPlayer + 1} — select a unit ({GetTerrain(Cursor)} @ {Cursor})";
        }
    }

    public void MoveCursor(int deltaX, int deltaY)
    {
        var x = Math.Clamp(Cursor.X + deltaX, 0, Width - 1);
        var y = Math.Clamp(Cursor.Y + deltaY, 0, Height - 1);
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

    private MatchUnit AddUnit(UnitKind kind, GridCell cell, int playerIndex)
    {
        var unit = new MatchUnit(_nextUnitId++, kind, cell, playerIndex);
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

        if (IsOccupiedByUnit(destination, exceptUnitId: unitId))
            return;

        unit.Cell = destination;
        SelectedUnitId = null;
    }

    private bool IsOccupiedByUnit(GridCell cell, int? exceptUnitId = null)
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
