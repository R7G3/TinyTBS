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
    private readonly List<GridCell> _gravestones = [];
    private readonly Dictionary<int, int> _moneyByPlayer = new();
    private int _nextUnitId;
    private int _playerCount = MatchDefaults.PlayerCount;

    private MatchState(int width, int height)
    {
        Width = width;
        Height = height;
        _terrain = new TerrainKind[width, height];
    }

    /// <summary>Builds match state from a loaded map; types must resolve in <paramref name="contentCatalog"/>.</summary>
    public static MatchState FromMap(
        MapDefinition map,
        MatchContentCatalog contentCatalog,
        int playerCount = MatchDefaults.PlayerCount,
        int startingGold = 0)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(contentCatalog);
        if (playerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(playerCount));

        var match = new MatchState(map.Width, map.Height)
        {
            Cursor = new GridCell(
                Math.Clamp(map.Width / 2, 0, Math.Max(0, map.Width - 1)),
                Math.Clamp(map.Height / 2, 0, Math.Max(0, map.Height - 1))),
            _playerCount = playerCount,
        };

        for (var playerIndex = 0; playerIndex < playerCount; playerIndex++)
            match._moneyByPlayer[playerIndex] = startingGold;

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
                match._terrain[x, y] = MapSurfaceIds.ParseTerrain(map.Surface[x, y]);
        }

        foreach (var building in map.Buildings)
        {
            if (!contentCatalog.TryGetBuilding(building.Type, out var buildingDefinition))
            {
                throw new InvalidOperationException(
                    $"Map building type '{building.Type.Full}' is not in the match content catalog.");
            }

            match._buildings.Add(new MatchBuilding(
                building.Type,
                new GridCell(building.X, building.Y),
                building.Slot,
                MapSurfaceIds.IsRuinedBuildingState(building.State),
                buildingDefinition.AllowsRecruit));
        }

        foreach (var unit in map.Units)
        {
            if (!contentCatalog.TryGetUnit(unit.Type, out var unitDefinition))
            {
                throw new InvalidOperationException(
                    $"Map unit type '{unit.Type.Full}' is not in the match content catalog.");
            }

            var maxHealth = unitDefinition.MaxHealth;
            var hitPoints = unit.Hp is int hitPointValue
                ? Math.Clamp(hitPointValue, 1, maxHealth)
                : maxHealth;

            match.AddUnit(
                unit.Type,
                new GridCell(unit.X, unit.Y),
                unit.Slot,
                maxHealth,
                hitPoints);
        }

        foreach (var gravestone in map.Gravestones)
            match._gravestones.Add(new GridCell(gravestone.X, gravestone.Y));

        return match;
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<MatchBuilding> Buildings => _buildings;

    public IReadOnlyList<MatchUnit> Units => _units;

    /// <summary>Placed gravestones from the map layer (visual + future combat rules).</summary>
    public IReadOnlyList<GridCell> Gravestones => _gravestones;

    public IReadOnlyDictionary<int, int> MoneyByPlayer => _moneyByPlayer;

    public int CurrentPlayer { get; private set; }

    /// <summary>Round number: increments when play returns to player 0.</summary>
    public int TurnNumber { get; private set; } = 1;

    public int? SelectedUnitId { get; private set; }

    public GridCell Cursor { get; private set; }

    public MatchPlayerAction? LastAction { get; private set; }

    public int? WinnerPlayerIndex { get; private set; }

    public string? VictoryReason { get; private set; }

    public TerrainKind GetTerrain(GridCell cell) => _terrain[cell.X, cell.Y];

    public TerrainKind GetTerrain(int x, int y) => _terrain[x, y];

    public int GetMoney(int playerIndex)
    {
        EnsureKnownPlayer(playerIndex);
        return _moneyByPlayer[playerIndex];
    }

    public void AddMoney(int playerIndex, int amount)
    {
        EnsureKnownPlayer(playerIndex);
        _moneyByPlayer[playerIndex] = checked(_moneyByPlayer[playerIndex] + amount);
    }

    public void SetVictory(int playerIndex, string reason)
    {
        EnsureKnownPlayer(playerIndex);
        WinnerPlayerIndex = playerIndex;
        VictoryReason = string.IsNullOrWhiteSpace(reason) ? "victory" : reason.Trim();
    }

    public string StatusText
    {
        get
        {
            if (WinnerPlayerIndex is int winner)
            {
                var reason = string.IsNullOrWhiteSpace(VictoryReason) ? "victory" : VictoryReason;
                return $"P{winner + 1} wins ({reason})";
            }

            var gold = GetMoney(CurrentPlayer);
            if (SelectedUnitId is int unitId && TryGetUnit(unitId, out var unit))
            {
                return $"P{CurrentPlayer + 1} · {gold}g · T{TurnNumber} — {unit.TypeId.Full} at {unit.Cell}";
            }

            return $"P{CurrentPlayer + 1} · {gold}g · T{TurnNumber} — {GetTerrain(Cursor)} @ {Cursor}";
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
        LastAction = null;

        if (SelectedUnitId is null)
        {
            TrySelectUnitAt(Cursor);
            return;
        }

        TryMoveSelectedUnitTo(Cursor);
    }

    /// <summary>Clears the selected unit (e.g. Esc / east cancel). Returns true if there was a selection.</summary>
    public bool ClearSelection()
    {
        if (SelectedUnitId is null)
            return false;

        SelectedUnitId = null;
        LastAction = null;
        return true;
    }

    public void HandlePointer(GridCell cell) => Cursor = cell;

    public void EndTurn()
    {
        LastAction = null;
        CurrentPlayer = (CurrentPlayer + 1) % _playerCount;
        if (CurrentPlayer == 0)
            TurnNumber++;
        SelectedUnitId = null;
    }

    public bool TryGetUnitAt(GridCell cell, out MatchUnit unit)
    {
        foreach (var candidate in _units)
        {
            if (candidate.Cell != cell)
                continue;

            unit = candidate;
            return true;
        }

        unit = null!;
        return false;
    }

    public bool TryGetBuildingAt(GridCell cell, out MatchBuilding building)
    {
        foreach (var candidate in _buildings)
        {
            if (candidate.Cell != cell)
                continue;

            building = candidate;
            return true;
        }

        building = null!;
        return false;
    }

    public bool IsOwnCastleAt(GridCell cell) =>
        TryGetBuildingAt(cell, out var building)
        && building.AllowsRecruit
        && !building.IsRuined
        && building.OwnerPlayerIndex == CurrentPlayer;

    /// <summary>
    /// Own castle with an own unit on it and nothing selected yet — Confirm should offer Move vs Buy.
    /// </summary>
    public bool NeedsCastleUnitActionChooser(GridCell cell) =>
        SelectedUnitId is null
        && IsOwnCastleAt(cell)
        && TryGetOwnUnitAt(cell, out _);

    public bool TryGetOwnUnitAt(GridCell cell, out MatchUnit unit)
    {
        if (!TryGetUnitAt(cell, out unit))
            return false;

        if (unit.PlayerIndex != CurrentPlayer)
        {
            unit = null!;
            return false;
        }

        return true;
    }

    /// <summary>Recruit a unit onto an owned recruit building cell if gold and space allow.</summary>
    public bool TryRecruitAtCastle(ContentId unitTypeId, int cost, int maxHealth, GridCell castleCell)
    {
        if (!IsOwnCastleAt(castleCell))
            return false;
        if (IsOccupiedByUnit(castleCell))
            return false;
        if (GetMoney(CurrentPlayer) < cost)
            return false;
        if (maxHealth <= 0)
            return false;

        AddMoney(CurrentPlayer, -cost);
        AddUnit(unitTypeId, castleCell, CurrentPlayer, maxHealth, maxHealth);
        LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.SelectUnit,
            PlayerIndex = CurrentPlayer,
            UnitId = _units[^1].Id,
            Source = castleCell,
            Target = castleCell,
        };
        SelectedUnitId = _units[^1].Id;
        return true;
    }

    private void EnsureKnownPlayer(int playerIndex)
    {
        if (!_moneyByPlayer.ContainsKey(playerIndex))
            throw new ArgumentOutOfRangeException(nameof(playerIndex), playerIndex, "Unknown player.");
    }

    private MatchUnit AddUnit(
        ContentId typeId,
        GridCell cell,
        int playerIndex,
        int maxHealth,
        int hitPoints)
    {
        var unit = new MatchUnit(_nextUnitId++, typeId, cell, playerIndex, maxHealth, hitPoints);
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
            LastAction = new MatchPlayerAction
            {
                Kind = MatchPlayerActionKind.SelectUnit,
                PlayerIndex = CurrentPlayer,
                UnitId = unit.Id,
                Source = cell,
                Target = cell,
            };
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

        var source = unit.Cell;
        unit.Cell = destination;
        SelectedUnitId = null;
        LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.MoveUnit,
            PlayerIndex = CurrentPlayer,
            UnitId = unitId,
            Source = source,
            Target = destination,
        };
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
