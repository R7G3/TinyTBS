using TinyTBS.Game.Maps;
using TinyTBS.Game.Maps.Models;

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

    /// <summary>Builds match state from a loaded map (vanilla/* ids → enums).</summary>
    public static MatchState FromMap(MapDefinition map)
    {
        ArgumentNullException.ThrowIfNull(map);

        var match = new MatchState(map.Width, map.Height)
        {
            Cursor = new GridCell(
                Math.Clamp(map.Width / 2, 0, Math.Max(0, map.Width - 1)),
                Math.Clamp(map.Height / 2, 0, Math.Max(0, map.Height - 1))),
        };

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
                match._terrain[x, y] = VanillaContentIds.ParseTerrain(map.Surface[x, y]);
        }

        foreach (var building in map.Buildings)
        {
            match._buildings.Add(new MatchBuilding(
                VanillaContentIds.ParseBuilding(building.Type),
                new GridCell(building.X, building.Y),
                building.Slot));
        }

        foreach (var unit in map.Units)
        {
            match.AddUnit(
                VanillaContentIds.ParseUnit(unit.Type),
                new GridCell(unit.X, unit.Y),
                unit.Slot);
        }

        // Memorials stay on MapDefinition for scripting; match rules do not use them yet.
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
