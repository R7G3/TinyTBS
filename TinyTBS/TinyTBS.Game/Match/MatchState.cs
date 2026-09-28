using TinyTBS.Game.Maps;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Match rules facade: activation, movement, and orchestration of combat / economy / overlays.
/// Details live in <see cref="MatchCombat"/>, <see cref="MatchEconomy"/>, <see cref="MatchUnitActionQueries"/>.
/// </summary>
public sealed class MatchState
{
    private readonly TerrainKind[,] _terrain;
    private readonly List<MatchBuilding> _buildings = [];
    private readonly List<MatchUnit> _units = [];
    private readonly List<MatchGravestone> _gravestones = [];
    private readonly Dictionary<int, int> _moneyByPlayer = new();
    private readonly Dictionary<int, int> _turnStartsByPlayer = new();
    private readonly Dictionary<int, int> _kingRehireCountByPlayer = new();
    private readonly HashSet<int> _eliminatedPlayers = [];
    private readonly MatchContentCatalog _catalog;
    private int _nextUnitId;
    private int _playerCount = MatchDefaults.PlayerCount;
    private string _victoryType = "standard";
    private string _defeatType = "standard";

    private MatchState(int width, int height, MatchContentCatalog catalog, int unitCap)
    {
        Width = width;
        Height = height;
        _terrain = new TerrainKind[width, height];
        _catalog = catalog;
        UnitCap = Math.Max(1, unitCap);
    }

    public static MatchState FromMap(
        MapDefinition map,
        MatchContentCatalog contentCatalog,
        ContentIdReplaceTable? replaces = null,
        int playerCount = MatchDefaults.PlayerCount,
        int startingGold = 0,
        int unitCap = 25,
        string victoryType = "standard",
        string defeatType = "standard")
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(contentCatalog);
        if (playerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(playerCount));

        replaces ??= ContentIdReplaceTable.Empty;

        var match = new MatchState(map.Width, map.Height, contentCatalog, unitCap)
        {
            Cursor = new GridCell(
                Math.Clamp(map.Width / 2, 0, Math.Max(0, map.Width - 1)),
                Math.Clamp(map.Height / 2, 0, Math.Max(0, map.Height - 1))),
            _playerCount = playerCount,
            _victoryType = string.IsNullOrWhiteSpace(victoryType) ? "standard" : victoryType.Trim(),
            _defeatType = string.IsNullOrWhiteSpace(defeatType) ? "standard" : defeatType.Trim(),
        };

        for (var playerIndex = 0; playerIndex < playerCount; playerIndex++)
        {
            match._moneyByPlayer[playerIndex] = startingGold;
            match._turnStartsByPlayer[playerIndex] = 0;
            match._kingRehireCountByPlayer[playerIndex] = 0;
        }

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
                match._terrain[x, y] = MapSurfaceIds.ParseTerrain(map.Surface[x, y]);
        }

        foreach (var building in map.Buildings)
        {
            var typeId = replaces.Resolve(building.Type);
            if (!contentCatalog.TryGetBuilding(typeId, out var buildingDefinition))
            {
                throw new InvalidOperationException(
                    $"Map building type '{building.Type.Full}' is not in the match content catalog.");
            }

            var ownerPlayerIndex = building.Slot is int buildingSlot && buildingSlot >= playerCount
                ? null
                : building.Slot;

            match._buildings.Add(new MatchBuilding(
                typeId,
                new GridCell(building.X, building.Y),
                ownerPlayerIndex,
                MapSurfaceIds.IsRuinedBuildingState(building.State),
                buildingDefinition.AllowsRecruit));
        }

        foreach (var unit in map.Units)
        {
            if (unit.Slot >= playerCount)
                continue;

            var typeId = replaces.Resolve(unit.Type);
            if (!contentCatalog.TryGetUnit(typeId, out var unitDefinition))
            {
                throw new InvalidOperationException(
                    $"Map unit type '{unit.Type.Full}' is not in the match content catalog.");
            }

            var maxHealth = unitDefinition.MaxHealth;
            var hitPoints = unit.Hp is int hitPointValue
                ? Math.Clamp(hitPointValue, 1, maxHealth)
                : maxHealth;

            match.SpawnUnit(typeId, new GridCell(unit.X, unit.Y), unit.Slot, maxHealth, hitPoints);
        }

        foreach (var gravestone in map.Gravestones)
        {
            match._gravestones.Add(new MatchGravestone(
                new GridCell(gravestone.X, gravestone.Y),
                sourcePlayerIndex: -1,
                expiresWhenTurnStartsReaches: int.MaxValue));
        }

        match.BeginCurrentPlayerTurn();
        match.EvaluateStandardOutcome();
        return match;
    }

    public int Width { get; }

    public int Height { get; }

    public int UnitCap { get; }

    public MatchContentCatalog ContentCatalog => _catalog;

    /// <summary>Internal alias for helpers in the Match assembly.</summary>
    internal MatchContentCatalog Catalog => _catalog;

    internal List<MatchUnit> UnitList => _units;

    internal List<MatchBuilding> BuildingList => _buildings;

    internal List<MatchGravestone> GravestoneList => _gravestones;

    internal Dictionary<int, int> KingRehireCounts => _kingRehireCountByPlayer;

    internal Dictionary<int, int> TurnStarts => _turnStartsByPlayer;

    public IReadOnlyList<MatchBuilding> Buildings => _buildings;

    public IReadOnlyList<MatchUnit> Units => _units;

    public IReadOnlyList<MatchGravestone> Gravestones => _gravestones;

    public IReadOnlyDictionary<int, int> MoneyByPlayer => _moneyByPlayer;

    public int CurrentPlayer { get; private set; }

    public int TurnNumber { get; private set; } = 1;

    public int? SelectedUnitId { get; internal set; }

    public GridCell Cursor { get; private set; }

    public MatchPlayerAction? LastAction { get; internal set; }

    public int? WinnerPlayerIndex { get; private set; }

    public string? VictoryReason { get; private set; }

    public bool IsMatchOver => WinnerPlayerIndex is not null;

    public bool IsPlayerEliminated(int playerIndex) => _eliminatedPlayers.Contains(playerIndex);

    public TerrainKind GetTerrain(GridCell cell) => _terrain[cell.X, cell.Y];

    public TerrainKind GetTerrain(int x, int y) => _terrain[x, y];

    public bool IsInBounds(GridCell cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    public bool IsOccupiedByUnitPublic(GridCell cell, int? exceptUnitId = null) =>
        IsOccupiedByUnit(cell, exceptUnitId);

    public int GetMoney(int playerIndex)
    {
        EnsureKnownPlayer(playerIndex);
        return _moneyByPlayer[playerIndex];
    }

    public int CountUnitsForPlayer(int playerIndex)
    {
        var count = 0;
        for (var i = 0; i < _units.Count; i++)
        {
            if (_units[i].PlayerIndex == playerIndex)
                count++;
        }

        return count;
    }

    public void AddMoney(int playerIndex, int amount)
    {
        EnsureKnownPlayer(playerIndex);
        _moneyByPlayer[playerIndex] = checked(_moneyByPlayer[playerIndex] + amount);
    }

    public void SetVictory(int playerIndex, string reason)
    {
        EnsureKnownPlayer(playerIndex);
        if (WinnerPlayerIndex is not null)
            return;

        WinnerPlayerIndex = playerIndex;
        VictoryReason = string.IsNullOrWhiteSpace(reason) ? "victory" : reason.Trim();
        SelectedUnitId = null;
        LastAction = null;
    }

    /// <summary>
    /// Standard GDD outcome: defeated = no uniquePerPlayer unit and no defeat-counting buildings;
    /// last living player wins. Custom victory/defeat types leave resolution to map scripts.
    /// </summary>
    public void EvaluateStandardOutcome()
    {
        if (WinnerPlayerIndex is not null)
            return;

        var useStandardDefeat = IsStandardCondition(_defeatType);
        var useStandardVictory = IsStandardCondition(_victoryType);
        if (!useStandardDefeat && !useStandardVictory)
            return;

        if (useStandardDefeat)
        {
            for (var playerIndex = 0; playerIndex < _playerCount; playerIndex++)
            {
                if (IsPlayerEliminated(playerIndex))
                    continue;
                if (IsPlayerStandardDefeated(playerIndex))
                    EliminatePlayer(playerIndex);
            }
        }

        if (useStandardVictory)
            TryDeclareLastLivingPlayerVictory();

        if (WinnerPlayerIndex is null
            && IsPlayerEliminated(CurrentPlayer)
            && CountLivingPlayers() > 0)
        {
            AdvancePastEliminatedPlayers();
        }
    }

    private static bool IsStandardCondition(string type) =>
        string.Equals(type, "standard", StringComparison.OrdinalIgnoreCase);

    private bool IsPlayerStandardDefeated(int playerIndex)
    {
        if (PlayerHasUniqueUnit(playerIndex))
            return false;
        if (PlayerHasDefeatCountingBuilding(playerIndex))
            return false;
        return true;
    }

    private bool PlayerHasUniqueUnit(int playerIndex)
    {
        foreach (var unit in _units)
        {
            if (unit.PlayerIndex != playerIndex)
                continue;
            if (!_catalog.TryGetUnit(unit.TypeId, out var definition))
                continue;
            if (MatchUnitAbilities.HasAbility(definition, "uniquePerPlayer"))
                return true;
        }

        return false;
    }

    private bool PlayerHasDefeatCountingBuilding(int playerIndex)
    {
        foreach (var building in _buildings)
        {
            if (building.OwnerPlayerIndex != playerIndex)
                continue;
            if (!_catalog.TryGetBuilding(building.TypeId, out var definition))
                continue;
            if (definition.CountsTowardPlayerDefeat)
                return true;
        }

        return false;
    }

    private void EliminatePlayer(int playerIndex)
    {
        if (!_eliminatedPlayers.Add(playerIndex))
            return;

        // GDD: units vanish without gravestones; buildings become neutral.
        for (var index = _units.Count - 1; index >= 0; index--)
        {
            var unit = _units[index];
            if (unit.PlayerIndex != playerIndex)
                continue;
            if (SelectedUnitId == unit.Id)
                SelectedUnitId = null;
            _units.RemoveAt(index);
        }

        foreach (var building in _buildings)
        {
            if (building.OwnerPlayerIndex == playerIndex)
                building.OwnerPlayerIndex = null;
        }
    }

    private int CountLivingPlayers()
    {
        var count = 0;
        for (var playerIndex = 0; playerIndex < _playerCount; playerIndex++)
        {
            if (!IsPlayerEliminated(playerIndex))
                count++;
        }

        return count;
    }

    private void TryDeclareLastLivingPlayerVictory()
    {
        if (CountLivingPlayers() != 1)
            return;

        for (var playerIndex = 0; playerIndex < _playerCount; playerIndex++)
        {
            if (IsPlayerEliminated(playerIndex))
                continue;
            SetVictory(playerIndex, "standard");
            return;
        }
    }

    private void AdvancePastEliminatedPlayers()
    {
        LastAction = null;
        SelectedUnitId = null;
        var guard = 0;
        do
        {
            CurrentPlayer = (CurrentPlayer + 1) % _playerCount;
            if (CurrentPlayer == 0)
                TurnNumber++;
            guard++;
        }
        while (IsPlayerEliminated(CurrentPlayer) && guard <= _playerCount);

        if (!IsPlayerEliminated(CurrentPlayer))
            BeginCurrentPlayerTurn();
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
            var army = CountUnitsForPlayer(CurrentPlayer);
            if (SelectedUnitId is int unitId && TryGetUnit(unitId, out var unit))
            {
                var active = unit.IsActive ? "ready" : "done";
                return $"P{CurrentPlayer + 1} · {gold}g · {army}/{UnitCap} · T{TurnNumber} — {unit.TypeId.LocalId} L{unit.Level} ({active})";
            }

            return $"P{CurrentPlayer + 1} · {gold}g · {army}/{UnitCap} · T{TurnNumber} — {GetTerrain(Cursor)} @ {Cursor}";
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
        if (IsMatchOver || IsPlayerEliminated(CurrentPlayer))
            return;

        LastAction = null;

        if (SelectedUnitId is null)
        {
            TrySelectUnitAt(Cursor);
            return;
        }

        if (!TryGetUnit(SelectedUnitId.Value, out var unit) || !unit.IsActive)
        {
            SelectedUnitId = null;
            return;
        }

        if (!_catalog.TryGetUnit(unit.TypeId, out var unitDefinition))
            return;

        // Confirm on own cell: capture / repair / wait.
        if (Cursor == unit.Cell)
        {
            if (TryCaptureOrRepairAtUnitCell(unit, unitDefinition))
                return;

            FinishUnitActivation(unit, MatchPlayerActionKind.WaitUnit);
            return;
        }

        // Confirm on enemy in attack range.
        if (TryGetUnitAt(Cursor, out var target)
            && target.PlayerIndex != CurrentPlayer
            && TryAttack(unit, unitDefinition, target))
        {
            return;
        }

        // Confirm on destroyable building in attack range (e.g. catapult → village).
        if (TryDestroyBuildingAtCursor(unit, unitDefinition))
            return;

        // Confirm on adjacent gravestone: raise skeleton (witch).
        if (TryRaiseSkeletonAtCursor(unit, unitDefinition))
            return;

        // Confirm on empty reachable cell: one move per activation (full Speed budget once).
        if (!unit.HasMovedThisActivation
            && !IsOccupiedByUnit(Cursor, exceptUnitId: unit.Id)
            && MatchPathfinder.CanReach(this, unit, Cursor, unitDefinition.MovementClass, unitDefinition.Speed, unit.Id))
        {
            TryMoveSelectedUnitTo(unit, unitDefinition, Cursor);
            return;
        }

        // Confirm on another own active unit: reselect.
        if (TryGetOwnUnitAt(Cursor, out var other) && other.IsActive)
        {
            SelectedUnitId = other.Id;
            LastAction = new MatchPlayerAction
            {
                Kind = MatchPlayerActionKind.SelectUnit,
                PlayerIndex = CurrentPlayer,
                UnitId = other.Id,
                Source = other.Cell,
                Target = other.Cell,
            };
        }
    }

    public bool ClearSelection()
    {
        if (SelectedUnitId is null)
            return false;

        if (TryGetUnit(SelectedUnitId.Value, out var unit) && unit.HasMovedThisActivation)
            UndoMove(unit);

        SelectedUnitId = null;
        LastAction = null;
        return true;
    }

    /// <summary>
    /// Ends the selected active unit's activation without attack/capture (face north / Y).
    /// </summary>
    public bool TryWaitSelectedUnit()
    {
        if (IsMatchOver || IsPlayerEliminated(CurrentPlayer))
            return false;
        if (SelectedUnitId is not int unitId || !TryGetUnit(unitId, out var unit) || !unit.IsActive)
            return false;

        FinishUnitActivation(unit, MatchPlayerActionKind.WaitUnit);
        return true;
    }

    public void HandlePointer(GridCell cell) => Cursor = cell;

    public void EndTurn()
    {
        if (IsMatchOver)
            return;

        LastAction = null;
        SelectedUnitId = null;
        foreach (var unit in _units)
        {
            if (unit.PlayerIndex == CurrentPlayer)
                unit.IsActive = false;
        }

        AdvancePastEliminatedPlayers();
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

    public bool NeedsCastleUnitActionChooser(GridCell cell) =>
        SelectedUnitId is null
        && IsOwnCastleAt(cell)
        && TryGetOwnUnitAt(cell, out var unit)
        && unit.IsActive;

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

    /// <summary>Recruit onto an owned recruit building cell if gold, cap, and uniqueness allow.</summary>
    public bool TryRecruitAtCastle(ContentId unitTypeId, int baseCost, int maxHealth, GridCell castleCell) =>
        MatchEconomy.TryRecruitAtCastle(this, unitTypeId, baseCost, maxHealth, castleCell);

    public int ResolveRecruitCost(ContentId unitTypeId, int baseCost) =>
        MatchEconomy.ResolveRecruitCost(this, unitTypeId, baseCost);

    private void BeginCurrentPlayerTurn() => MatchEconomy.BeginCurrentPlayerTurn(this);

    /// <summary>
    /// Reachable cells for the selected active unit that has not moved yet; empty otherwise.
    /// </summary>
    public IReadOnlyList<GridCell> GetSelectedUnitMoveRange()
    {
        if (!TryGetSelectedUnitActionOverlay(out var overlay))
            return [];
        return overlay.MoveCells;
    }

    /// <summary>
    /// Enemy unit cells and destroyable buildings the selected unit can act on.
    /// </summary>
    public IReadOnlyList<GridCell> GetSelectedUnitAttackTargets()
    {
        if (!TryGetSelectedUnitActionOverlay(out var overlay))
            return [];
        return overlay.AttackCells;
    }

    /// <summary>
    /// Adjacent gravestone cells the selected raiseSkeleton unit can raise from.
    /// </summary>
    public IReadOnlyList<GridCell> GetSelectedUnitRaiseTargets()
    {
        if (!TryGetSelectedUnitActionOverlay(out var overlay))
            return [];
        return overlay.RaiseCells;
    }

    /// <summary>
    /// Action overlay for the selected active unit (move / attack / capture / repair / raise).
    /// After a move this turn, move cells are empty and actions are from the current cell only.
    /// </summary>
    public bool TryGetSelectedUnitActionOverlay(out MatchUnitActionOverlay overlay)
    {
        overlay = null!;
        if (!TryGetSelectedActiveUnit(out var unit, out var definition))
            return false;

        overlay = MatchUnitActionQueries.Build(this, unit, definition, respectActivationMove: true);
        return true;
    }

    /// <summary>
    /// Next-turn threat overlay for any living unit (informational; does not select or act).
    /// </summary>
    public bool TryGetUnitThreatPreview(int unitId, out MatchUnitActionOverlay overlay)
    {
        overlay = null!;
        if (!TryGetUnit(unitId, out var unit))
            return false;
        if (!_catalog.TryGetUnit(unit.TypeId, out var definition))
            return false;

        overlay = MatchUnitActionQueries.Build(this, unit, definition, respectActivationMove: false);
        return true;
    }

    private bool TryGetSelectedActiveUnit(out MatchUnit unit, out UnitDefinition definition)
    {
        definition = null!;
        unit = null!;
        if (SelectedUnitId is not int unitId || !TryGetUnit(unitId, out unit) || !unit.IsActive)
            return false;
        if (!_catalog.TryGetUnit(unit.TypeId, out definition))
            return false;
        return true;
    }

    /// <summary>
    /// Whether the current player already has a living unit of a <c>uniquePerPlayer</c> type
    /// (e.g. king) — shop should hide that offer.
    /// </summary>
    public bool IsUniqueUnitOwnedByCurrentPlayer(ContentId unitTypeId)
    {
        if (!_catalog.TryGetUnit(unitTypeId, out var definition))
            return false;
        if (!MatchUnitAbilities.HasAbility(definition, "uniquePerPlayer"))
            return false;

        return _units.Any(unit => unit.PlayerIndex == CurrentPlayer && unit.TypeId == unitTypeId);
    }

    private bool TryMoveSelectedUnitTo(MatchUnit unit, UnitDefinition definition, GridCell destination)
    {
        if (unit.HasMovedThisActivation)
            return false;

        var source = unit.Cell;
        unit.CellBeforeMove = source;
        unit.Cell = destination;
        unit.HasMovedThisActivation = true;
        LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.MoveUnit,
            PlayerIndex = CurrentPlayer,
            UnitId = unit.Id,
            Source = source,
            Target = destination,
        };

        if (MatchUnitAbilities.HasAbility(definition, "moveOrAttackExclusive"))
        {
            FinishUnitActivation(unit, MatchPlayerActionKind.MoveUnit);
            return true;
        }

        SelectedUnitId = unit.Id;
        return true;
    }

    private bool TryAttack(MatchUnit attacker, UnitDefinition attackerDefinition, MatchUnit defender) =>
        MatchCombat.TryAttack(this, attacker, attackerDefinition, defender);

    private bool TryDestroyBuildingAtCursor(MatchUnit unit, UnitDefinition unitDefinition) =>
        MatchCombat.TryDestroyBuildingAtCursor(this, unit, unitDefinition);

    private bool TryRaiseSkeletonAtCursor(MatchUnit unit, UnitDefinition unitDefinition)
    {
        if (!MatchUnitAbilities.HasAbility(unitDefinition, "raiseSkeleton"))
            return false;
        if (unit.Cell.ManhattanDistanceTo(Cursor) != 1)
            return false;
        if (IsOccupiedByUnit(Cursor))
            return false;
        if (CountUnitsForPlayer(CurrentPlayer) >= UnitCap)
            return false;

        var stoneIndex = _gravestones.FindIndex(stone => stone.Cell == Cursor);
        if (stoneIndex < 0)
            return false;

        var skeletonTypeId = new ContentId(unit.TypeId.Namespace, "skeleton");
        if (!_catalog.TryGetUnit(skeletonTypeId, out var skeletonDefinition))
            return false;

        _gravestones.RemoveAt(stoneIndex);
        var spawned = SpawnUnit(
            skeletonTypeId,
            Cursor,
            CurrentPlayer,
            skeletonDefinition.MaxHealth,
            skeletonDefinition.MaxHealth);
        spawned.IsActive = false;

        LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.RaiseSkeleton,
            PlayerIndex = CurrentPlayer,
            UnitId = unit.Id,
            Source = unit.Cell,
            Target = Cursor,
        };
        FinishUnitActivation(unit, MatchPlayerActionKind.RaiseSkeleton);
        return true;
    }

    /// <summary>
    /// Best (max) attackAura value covering <paramref name="cell"/> for <paramref name="playerIndex"/>.
    /// </summary>
    public int GetAttackAuraBonus(GridCell cell, int playerIndex) =>
        MatchCombat.ResolveAttackAuraBonus(this, cell, playerIndex);

    private bool TryCaptureOrRepairAtUnitCell(MatchUnit unit, UnitDefinition unitDefinition)
    {
        if (!TryGetBuildingAt(unit.Cell, out var building))
            return false;
        if (!_catalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
            return false;

        if (building.IsRuined
            && buildingDefinition.Repairable
            && MatchUnitAbilities.CanRepair(unitDefinition, buildingDefinition))
        {
            building.IsRuined = false;
            building.OwnerPlayerIndex = null;
            building.RepairedThisOwnerTurn = true;
            LastAction = new MatchPlayerAction
            {
                Kind = MatchPlayerActionKind.RepairBuilding,
                PlayerIndex = CurrentPlayer,
                UnitId = unit.Id,
                Source = unit.Cell,
                Target = unit.Cell,
            };
            FinishUnitActivation(unit, MatchPlayerActionKind.RepairBuilding);
            return true;
        }

        if (!building.IsRuined
            && !building.RepairedThisOwnerTurn
            && MatchUnitAbilities.IsCapturable(building, buildingDefinition)
            && building.OwnerPlayerIndex != CurrentPlayer
            && MatchUnitAbilities.CanCapture(unitDefinition, buildingDefinition))
        {
            building.OwnerPlayerIndex = CurrentPlayer;
            LastAction = new MatchPlayerAction
            {
                Kind = MatchPlayerActionKind.CaptureBuilding,
                PlayerIndex = CurrentPlayer,
                UnitId = unit.Id,
                Source = unit.Cell,
                Target = unit.Cell,
            };
            FinishUnitActivation(unit, MatchPlayerActionKind.CaptureBuilding);
            return true;
        }

        return false;
    }

    private void UndoMove(MatchUnit unit)
    {
        if (!unit.HasMovedThisActivation)
            return;

        if (!IsOccupiedByUnit(unit.CellBeforeMove, exceptUnitId: unit.Id))
            unit.Cell = unit.CellBeforeMove;

        unit.HasMovedThisActivation = false;
    }

    internal void FinishUnitActivation(MatchUnit unit, MatchPlayerActionKind kind)
    {
        unit.IsActive = false;
        SelectedUnitId = null;
        if (LastAction is null)
        {
            LastAction = new MatchPlayerAction
            {
                Kind = kind,
                PlayerIndex = CurrentPlayer,
                UnitId = unit.Id,
                Source = unit.Cell,
                Target = unit.Cell,
            };
        }
    }

    private void EnsureKnownPlayer(int playerIndex)
    {
        if (!_moneyByPlayer.ContainsKey(playerIndex))
            throw new ArgumentOutOfRangeException(nameof(playerIndex), playerIndex, "Unknown player.");
    }

    internal MatchUnit SpawnUnit(
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

            if (unit.PlayerIndex != CurrentPlayer || !unit.IsActive)
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

