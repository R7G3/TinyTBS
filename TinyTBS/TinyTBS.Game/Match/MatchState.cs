using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Maps;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Match rules: activation, path move by Speed, attack / capture / repair / destroy / raise,
/// turn-start income+heal, recruit with unit cap. Canon: COMBAT / WORLD / TURN_AND_UI.
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

            match.AddUnit(typeId, new GridCell(unit.X, unit.Y), unit.Slot, maxHealth, hitPoints);
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

    public IReadOnlyList<MatchBuilding> Buildings => _buildings;

    public IReadOnlyList<MatchUnit> Units => _units;

    public IReadOnlyList<MatchGravestone> Gravestones => _gravestones;

    public IReadOnlyDictionary<int, int> MoneyByPlayer => _moneyByPlayer;

    public int CurrentPlayer { get; private set; }

    public int TurnNumber { get; private set; } = 1;

    public int? SelectedUnitId { get; private set; }

    public GridCell Cursor { get; private set; }

    public MatchPlayerAction? LastAction { get; private set; }

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
            if (HasAbility(definition, "uniquePerPlayer"))
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
    public bool TryRecruitAtCastle(ContentId unitTypeId, int baseCost, int maxHealth, GridCell castleCell)
    {
        if (IsMatchOver || IsPlayerEliminated(CurrentPlayer))
            return false;
        if (!IsOwnCastleAt(castleCell))
            return false;
        if (IsOccupiedByUnit(castleCell))
            return false;
        if (CountUnitsForPlayer(CurrentPlayer) >= UnitCap)
            return false;
        if (maxHealth <= 0)
            return false;
        if (!_catalog.TryGetUnit(unitTypeId, out var definition))
            return false;

        if (HasAbility(definition, "uniquePerPlayer")
            && _units.Any(unit => unit.PlayerIndex == CurrentPlayer && unit.TypeId == unitTypeId))
        {
            return false;
        }

        var cost = ResolveRecruitCost(unitTypeId, baseCost);

        if (GetMoney(CurrentPlayer) < cost)
            return false;

        AddMoney(CurrentPlayer, -cost);
        var spawned = AddUnit(unitTypeId, castleCell, CurrentPlayer, maxHealth, maxHealth);
        spawned.IsActive = true;
        LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.RecruitUnit,
            PlayerIndex = CurrentPlayer,
            UnitId = spawned.Id,
            Source = castleCell,
            Target = castleCell,
        };
        SelectedUnitId = spawned.Id;
        return true;
    }

    public int ResolveRecruitCost(ContentId unitTypeId, int baseCost)
    {
        if (!_catalog.TryGetUnit(unitTypeId, out var definition))
            return baseCost;

        if (TryGetAbility(definition, "rehireCostIncrement", out var rehire)
            && rehire.Amount is int increment)
        {
            return baseCost + _kingRehireCountByPlayer[CurrentPlayer] * increment;
        }

        return baseCost;
    }

    private void BeginCurrentPlayerTurn()
    {
        _turnStartsByPlayer[CurrentPlayer]++;
        ExpireGravestonesForCurrentPlayer();

        foreach (var building in _buildings)
            building.RepairedThisOwnerTurn = false;

        foreach (var unit in _units)
        {
            if (unit.PlayerIndex != CurrentPlayer)
                continue;
            unit.IsActive = true;
            unit.HasMovedThisActivation = false;
            unit.CellBeforeMove = unit.Cell;
        }

        // Income + heal from the player's second turn onward (WORLD.md).
        if (_turnStartsByPlayer[CurrentPlayer] >= 2)
            ApplyIncomeAndHeal(CurrentPlayer);
    }

    private void ExpireGravestonesForCurrentPlayer()
    {
        var turnStarts = _turnStartsByPlayer[CurrentPlayer];
        _gravestones.RemoveAll(stone =>
            stone.SourcePlayerIndex == CurrentPlayer
            && turnStarts >= stone.ExpiresWhenTurnStartsReaches);
    }

    private void ApplyIncomeAndHeal(int playerIndex)
    {
        foreach (var building in _buildings)
        {
            if (building.OwnerPlayerIndex != playerIndex)
                continue;
            if (!_catalog.TryGetBuilding(building.TypeId, out var definition))
                continue;

            var income = building.IsRuined
                ? definition.Ruined?.Income ?? 0
                : definition.Income;
            if (income > 0)
                AddMoney(playerIndex, income);

            var healAmount = ResolveHealAmount(definition, building.IsRuined);
            if (healAmount <= 0)
                continue;

            if (!TryGetUnitAt(building.Cell, out var occupant) || occupant.PlayerIndex != playerIndex)
                continue;

            occupant.HitPoints = Math.Min(occupant.MaxHealth, occupant.HitPoints + healAmount);
        }
    }

    private static int ResolveHealAmount(BuildingDefinition definition, bool isRuined)
    {
        if (isRuined)
            return definition.Ruined?.Heal?.Amount ?? 0;
        return definition.Heal?.Amount ?? 0;
    }

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

        overlay = BuildActionOverlay(unit, definition, respectActivationMove: true);
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

        overlay = BuildActionOverlay(unit, definition, respectActivationMove: false);
        return true;
    }

    private MatchUnitActionOverlay BuildActionOverlay(
        MatchUnit unit,
        UnitDefinition definition,
        bool respectActivationMove)
    {
        var hasMoved = respectActivationMove && unit.HasMovedThisActivation;
        var moveCells = hasMoved ? (IReadOnlyList<GridCell>)[] : CollectMoveRange(unit, definition);

        var standCells = new List<GridCell>(moveCells.Count + 1) { unit.Cell };
        foreach (var cell in moveCells)
            standCells.Add(cell);

        IReadOnlyList<GridCell> attackStands;
        if (HasAbility(definition, "moveOrAttackExclusive"))
        {
            // Exclusive: attack only from current cell, and only if this activation has not moved.
            attackStands = hasMoved ? [] : [unit.Cell];
        }
        else
        {
            attackStands = standCells;
        }

        return new MatchUnitActionOverlay
        {
            MoveCells = moveCells,
            AttackCells = CollectAttackTargetsFromStands(unit, definition, attackStands),
            CaptureCells = CollectCaptureTargetsFromStands(unit, definition, standCells),
            RepairCells = CollectRepairTargetsFromStands(unit, definition, standCells),
            RaiseCells = CollectRaiseTargetsFromStands(unit, definition, standCells),
        };
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

    private IReadOnlyList<GridCell> CollectMoveRange(MatchUnit unit, UnitDefinition definition) =>
        MatchPathfinder.CollectReachable(
            this,
            unit,
            definition.MovementClass,
            definition.Speed,
            unit.Id);

    private IReadOnlyList<GridCell> CollectAttackTargetsFromStands(
        MatchUnit unit,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        var targets = new HashSet<GridCell>();
        foreach (var stand in standCells)
            CollectAttackTargetsFromCell(unit, definition, stand, targets);

        return targets.Count == 0 ? [] : targets.ToArray();
    }

    private void CollectAttackTargetsFromCell(
        MatchUnit unit,
        UnitDefinition definition,
        GridCell fromCell,
        HashSet<GridCell> targets)
    {
        foreach (var candidate in _units)
        {
            if (candidate.Id == unit.Id || candidate.PlayerIndex == unit.PlayerIndex)
                continue;

            var range = fromCell.ManhattanDistanceTo(candidate.Cell);
            if (range < definition.AttackRangeMin || range > definition.AttackRangeMax)
                continue;

            targets.Add(candidate.Cell);
        }

        if (!TryGetAbility(definition, "destroyBuilding", out var destroyAbility))
            return;

        foreach (var building in _buildings)
        {
            if (building.IsRuined)
                continue;
            if (!_catalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
                continue;
            if (!buildingDefinition.Destroyable)
                continue;
            if (!TagsIntersect(destroyAbility.Tags, buildingDefinition.Tags))
                continue;
            if (IsOccupiedByUnit(building.Cell, exceptUnitId: unit.Id))
                continue;

            var range = fromCell.ManhattanDistanceTo(building.Cell);
            if (range < definition.AttackRangeMin || range > definition.AttackRangeMax)
                continue;

            targets.Add(building.Cell);
        }
    }

    private IReadOnlyList<GridCell> CollectCaptureTargetsFromStands(
        MatchUnit unit,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        var targets = new List<GridCell>();
        foreach (var stand in standCells)
        {
            if (!TryGetBuildingAt(stand, out var building))
                continue;
            if (!_catalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
                continue;
            if (!IsCapturable(building, buildingDefinition))
                continue;
            if (building.OwnerPlayerIndex == unit.PlayerIndex)
                continue;
            if (!CanCapture(definition, buildingDefinition))
                continue;

            targets.Add(stand);
        }

        return targets;
    }

    private IReadOnlyList<GridCell> CollectRepairTargetsFromStands(
        MatchUnit unit,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        var targets = new List<GridCell>();
        foreach (var stand in standCells)
        {
            if (!TryGetBuildingAt(stand, out var building))
                continue;
            if (!building.IsRuined)
                continue;
            if (!_catalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
                continue;
            if (!buildingDefinition.Repairable)
                continue;
            if (!CanRepair(definition, buildingDefinition))
                continue;

            targets.Add(stand);
        }

        return targets;
    }

    private IReadOnlyList<GridCell> CollectRaiseTargetsFromStands(
        MatchUnit unit,
        UnitDefinition definition,
        IReadOnlyList<GridCell> standCells)
    {
        if (!HasAbility(definition, "raiseSkeleton"))
            return [];
        if (CountUnitsForPlayer(unit.PlayerIndex) >= UnitCap)
            return [];

        var targets = new HashSet<GridCell>();
        foreach (var stand in standCells)
        {
            foreach (var stone in _gravestones)
            {
                if (stand.ManhattanDistanceTo(stone.Cell) != 1)
                    continue;
                if (IsOccupiedByUnit(stone.Cell, exceptUnitId: unit.Id))
                    continue;

                targets.Add(stone.Cell);
            }
        }

        return targets.Count == 0 ? [] : targets.ToArray();
    }

    /// <summary>
    /// Whether the current player already has a living unit of a <c>uniquePerPlayer</c> type
    /// (e.g. king) — shop should hide that offer.
    /// </summary>
    public bool IsUniqueUnitOwnedByCurrentPlayer(ContentId unitTypeId)
    {
        if (!_catalog.TryGetUnit(unitTypeId, out var definition))
            return false;
        if (!HasAbility(definition, "uniquePerPlayer"))
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

        // Exclusive movers finish after moving.
        if (HasAbility(definition, "moveOrAttackExclusive"))
        {
            FinishUnitActivation(unit, MatchPlayerActionKind.MoveUnit);
            return true;
        }

        // Keep selection so the player can attack / capture / wait on cells.
        SelectedUnitId = unit.Id;
        return true;
    }

    private bool TryAttack(MatchUnit attacker, UnitDefinition attackerDefinition, MatchUnit defender)
    {
        if (HasAbility(attackerDefinition, "moveOrAttackExclusive") && attacker.HasMovedThisActivation)
            return false;

        var range = attacker.Cell.ManhattanDistanceTo(defender.Cell);
        if (range < attackerDefinition.AttackRangeMin || range > attackerDefinition.AttackRangeMax)
            return false;
        if (!_catalog.TryGetUnit(defender.TypeId, out var defenderDefinition))
            return false;

        var terrainDef = MatchTerrainRules.DefenceBonus(GetTerrain(defender.Cell));
        var buildingDef = ResolveBuildingDefenceBonus(defender.Cell);
        var attackAura = ResolveAttackAuraBonus(attacker.Cell, attacker.PlayerIndex);
        var damage = MatchCombatFormula.ComputeDamage(
            attackerDefinition,
            attacker.Level,
            attacker.HitPoints,
            defenderDefinition,
            defender.Level,
            terrainDef,
            buildingDef,
            range,
            attackAura);

        ApplyDamage(defender, defenderDefinition, damage);
        var defenderDied = !_units.Contains(defender);

        if (!defenderDied)
            attacker.GainExperience(1);
        else
            attacker.GainExperience(2);

        // Counterattack: adjacent only, unless abilities forbid.
        if (!defenderDied
            && range == 1
            && !HasAbility(defenderDefinition, "noCounterattack")
            && !SuppressesCounterattack(attackerDefinition, range))
        {
            var counterTerrain = MatchTerrainRules.DefenceBonus(GetTerrain(attacker.Cell));
            var counterBuilding = ResolveBuildingDefenceBonus(attacker.Cell);
            var counterAura = ResolveAttackAuraBonus(defender.Cell, defender.PlayerIndex);
            var counterDamage = MatchCombatFormula.ComputeDamage(
                defenderDefinition,
                defender.Level,
                defender.HitPoints,
                attackerDefinition,
                attacker.Level,
                counterTerrain,
                counterBuilding,
                range,
                counterAura);
            ApplyDamage(attacker, attackerDefinition, counterDamage);
            if (_units.Contains(attacker))
                defender.GainExperience(counterDamage > 0 && attacker.HitPoints > 0 ? 1 : 2);
        }

        LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.AttackUnit,
            PlayerIndex = CurrentPlayer,
            UnitId = attacker.Id,
            Source = attacker.Cell,
            Target = defender.Cell,
        };

        if (_units.Contains(attacker))
            FinishUnitActivation(attacker, MatchPlayerActionKind.AttackUnit);
        else
            SelectedUnitId = null;

        return true;
    }

    private static bool SuppressesCounterattack(UnitDefinition attacker, int range) =>
        attacker.Abilities.Any(ability =>
            string.Equals(ability.Type, "noCounterattackWhenRangeAtLeast", StringComparison.OrdinalIgnoreCase)
            && ability.MinRange is int min
            && range >= min);

    private int ResolveBuildingDefenceBonus(GridCell cell)
    {
        if (!TryGetBuildingAt(cell, out var building))
            return 0;
        if (!_catalog.TryGetBuilding(building.TypeId, out var definition))
            return 0;

        if (building.IsRuined)
            return definition.Ruined?.DefenceBonus ?? 0;
        return definition.DefenceBonus;
    }

    private void ApplyDamage(MatchUnit unit, UnitDefinition definition, int damage)
    {
        if (damage <= 0)
            return;

        unit.HitPoints -= damage;
        if (unit.HitPoints > 0)
            return;

        var cell = unit.Cell;
        var playerIndex = unit.PlayerIndex;
        if (HasAbility(definition, "uniquePerPlayer")
            || HasAbility(definition, "rehireCostIncrement"))
        {
            _kingRehireCountByPlayer[playerIndex] = _kingRehireCountByPlayer.GetValueOrDefault(playerIndex) + 1;
        }

        _units.Remove(unit);
        if (SelectedUnitId == unit.Id)
            SelectedUnitId = null;

        if (definition.LeavesGravestone && !TryGetBuildingAt(cell, out _))
        {
            if (!_gravestones.Any(stone => stone.Cell == cell))
            {
                var expireAt = _turnStartsByPlayer.GetValueOrDefault(playerIndex) + 2;
                _gravestones.Add(new MatchGravestone(cell, playerIndex, expireAt));
            }
        }
    }

    private bool TryDestroyBuildingAtCursor(MatchUnit unit, UnitDefinition unitDefinition)
    {
        if (HasAbility(unitDefinition, "moveOrAttackExclusive") && unit.HasMovedThisActivation)
            return false;
        if (!TryGetAbility(unitDefinition, "destroyBuilding", out var destroyAbility))
            return false;
        if (!TryGetBuildingAt(Cursor, out var building) || building.IsRuined)
            return false;
        if (!_catalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
            return false;
        if (!buildingDefinition.Destroyable)
            return false;
        if (!TagsIntersect(destroyAbility.Tags, buildingDefinition.Tags))
            return false;
        if (IsOccupiedByUnit(building.Cell))
            return false;

        var range = unit.Cell.ManhattanDistanceTo(building.Cell);
        if (range < unitDefinition.AttackRangeMin || range > unitDefinition.AttackRangeMax)
            return false;

        // Vanilla villages: one-shot to ruined (no building HP in definition yet).
        building.IsRuined = true;
        LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.DestroyBuilding,
            PlayerIndex = CurrentPlayer,
            UnitId = unit.Id,
            Source = unit.Cell,
            Target = building.Cell,
        };
        FinishUnitActivation(unit, MatchPlayerActionKind.DestroyBuilding);
        return true;
    }

    private bool TryRaiseSkeletonAtCursor(MatchUnit unit, UnitDefinition unitDefinition)
    {
        if (!HasAbility(unitDefinition, "raiseSkeleton"))
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
        var spawned = AddUnit(
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
        ResolveAttackAuraBonus(cell, playerIndex);

    /// <summary>
    /// Best (max) attackAura value from allied units whose aura covers <paramref name="cell"/>.
    /// </summary>
    private int ResolveAttackAuraBonus(GridCell cell, int playerIndex)
    {
        var best = 0;
        foreach (var ally in _units)
        {
            if (ally.PlayerIndex != playerIndex)
                continue;
            if (!_catalog.TryGetUnit(ally.TypeId, out var definition))
                continue;
            if (!TryGetAbility(definition, "attackAura", out var aura))
                continue;

            var radius = aura.Radius ?? 0;
            var value = aura.Value ?? 0;
            if (radius <= 0 || value <= 0)
                continue;
            if (ally.Cell.ManhattanDistanceTo(cell) > radius)
                continue;

            if (value > best)
                best = value;
        }

        return best;
    }

    private static bool TagsIntersect(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        foreach (var leftTag in left)
        {
            foreach (var rightTag in right)
            {
                if (string.Equals(leftTag, rightTag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private bool TryCaptureOrRepairAtUnitCell(MatchUnit unit, UnitDefinition unitDefinition)
    {
        if (!TryGetBuildingAt(unit.Cell, out var building))
            return false;
        if (!_catalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
            return false;

        if (building.IsRuined
            && buildingDefinition.Repairable
            && CanRepair(unitDefinition, buildingDefinition))
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
            && IsCapturable(building, buildingDefinition)
            && building.OwnerPlayerIndex != CurrentPlayer
            && CanCapture(unitDefinition, buildingDefinition))
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

    private static bool IsCapturable(MatchBuilding building, BuildingDefinition definition)
    {
        if (building.IsRuined)
            return definition.Ruined?.Capturable ?? false;
        return definition.Capturable;
    }

    private static bool CanCapture(UnitDefinition unit, BuildingDefinition building) =>
        unit.Abilities.Any(ability =>
            string.Equals(ability.Type, "captureBuilding", StringComparison.OrdinalIgnoreCase)
            && ability.Tags.Any(tag =>
                building.Tags.Any(buildingTag =>
                    string.Equals(tag, buildingTag, StringComparison.OrdinalIgnoreCase))));

    private static bool CanRepair(UnitDefinition unit, BuildingDefinition building) =>
        unit.Abilities.Any(ability =>
            string.Equals(ability.Type, "repairBuilding", StringComparison.OrdinalIgnoreCase)
            && ability.Tags.Any(tag =>
                building.Tags.Any(buildingTag =>
                    string.Equals(tag, buildingTag, StringComparison.OrdinalIgnoreCase))));

    private void UndoMove(MatchUnit unit)
    {
        if (!unit.HasMovedThisActivation)
            return;

        // Destination must be free for undo; if another unit somehow occupies origin, stay put.
        if (!IsOccupiedByUnit(unit.CellBeforeMove, exceptUnitId: unit.Id))
            unit.Cell = unit.CellBeforeMove;

        unit.HasMovedThisActivation = false;
    }

    private void FinishUnitActivation(MatchUnit unit, MatchPlayerActionKind kind)
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

    private static bool HasAbility(UnitDefinition definition, string type) =>
        definition.Abilities.Any(ability =>
            string.Equals(ability.Type, type, StringComparison.OrdinalIgnoreCase));

    private static bool TryGetAbility(UnitDefinition definition, string type, out UnitAbilityDefinition ability)
    {
        foreach (var candidate in definition.Abilities)
        {
            if (!string.Equals(candidate.Type, type, StringComparison.OrdinalIgnoreCase))
                continue;
            ability = candidate;
            return true;
        }

        ability = null!;
        return false;
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

