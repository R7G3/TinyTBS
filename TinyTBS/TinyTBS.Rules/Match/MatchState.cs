using TinyTBS.Rules.Maps;
using TinyTBS.Rules.Maps.Models;
using TinyTBS.Rules.Modules;
using TinyTBS.Rules.Saves.Models;
using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Rules.Match;

/// <summary>
/// Match rules state: terrain, units, buildings, economy, turn order and victory.
/// Commands arrive as <see cref="MatchAction"/>s (<see cref="TryApply"/>) or as clicks (<see cref="ConfirmAt"/>);
/// legality lives in <see cref="MatchActionRules"/>. Holds no cursor, unit selection or other presentation state.
/// Callers pass the current selection in and keep the one returned by <see cref="MatchApplyResult"/>.
/// </summary>
public sealed class MatchState
{
    /// <summary>Immutable after <see cref="FromMap"/>; clones share it.</summary>
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
    private string _victoryType = MatchConditionTypes.Standard;
    private string _defeatType = MatchConditionTypes.Standard;

    private MatchState(TerrainKind[,] terrain, MatchContentCatalog catalog, int unitCap)
    {
        _terrain = terrain;
        Width = terrain.GetLength(0);
        Height = terrain.GetLength(1);
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
        string victoryType = MatchConditionTypes.Standard,
        string defeatType = MatchConditionTypes.Standard)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(contentCatalog);
        if (playerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(playerCount));

        replaces ??= ContentIdReplaceTable.Empty;

        var terrain = new TerrainKind[map.Width, map.Height];
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
                terrain[x, y] = MapSurfaceIds.ParseTerrain(map.Surface[x, y]);
        }

        var match = new MatchState(terrain, contentCatalog, unitCap)
        {
            _playerCount = playerCount,
            _victoryType = MatchConditionTypes.Normalize(victoryType),
            _defeatType = MatchConditionTypes.Normalize(defeatType),
        };

        for (var playerIndex = 0; playerIndex < playerCount; playerIndex++)
        {
            match._moneyByPlayer[playerIndex] = startingGold;
            match._turnStartsByPlayer[playerIndex] = 0;
            match._kingRehireCountByPlayer[playerIndex] = 0;
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

            var spawned = match.SpawnUnit(typeId, new GridCell(unit.X, unit.Y), unit.Slot, maxHealth, hitPoints);
            if (unit.Xp is int experience)
                spawned.Experience = Math.Clamp(experience, 0, MatchUnit.MaxExperience);
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

    public int PlayerCount => _playerCount;

    public MatchContentCatalog ContentCatalog => _catalog;

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

    public MatchPlayerAction? LastAction { get; internal set; }

    /// <summary>
    /// Drops a selection the rules no longer accept: match over, or the unit is gone / inactive.
    /// A unit that already moved this activation is still selected — that flag lives on the unit.
    /// </summary>
    public int? NormalizeSelection(int? selectedUnitId)
    {
        if (IsMatchOver || selectedUnitId is not int unitId)
            return null;
        if (!TryGetUnit(unitId, out var unit) || !unit.IsActive || unit.PlayerIndex != CurrentPlayer)
            return null;
        return unitId;
    }

    /// <summary>Current player's unit that has already moved and still needs to act or undo.</summary>
    public int? PendingActivationUnitId()
    {
        foreach (var unit in _units)
        {
            if (unit.PlayerIndex == CurrentPlayer && unit.IsActive && unit.HasMovedThisActivation)
                return unit.Id;
        }

        return null;
    }

    public int? WinnerPlayerIndex { get; private set; }

    public string? VictoryReason { get; private set; }

    public bool IsMatchOver => WinnerPlayerIndex is not null;

    public bool IsPlayerEliminated(int playerIndex) => _eliminatedPlayers.Contains(playerIndex);

    /// <summary>Deep copy for bot search; shares the immutable terrain and content catalog.</summary>
    public MatchState Clone()
    {
        var clone = new MatchState(_terrain, _catalog, UnitCap)
        {
            _playerCount = _playerCount,
            _victoryType = _victoryType,
            _defeatType = _defeatType,
            _nextUnitId = _nextUnitId,
            CurrentPlayer = CurrentPlayer,
            TurnNumber = TurnNumber,
            LastAction = LastAction,
            WinnerPlayerIndex = WinnerPlayerIndex,
            VictoryReason = VictoryReason,
        };

        foreach (var pair in _moneyByPlayer)
            clone._moneyByPlayer[pair.Key] = pair.Value;
        foreach (var pair in _turnStartsByPlayer)
            clone._turnStartsByPlayer[pair.Key] = pair.Value;
        foreach (var pair in _kingRehireCountByPlayer)
            clone._kingRehireCountByPlayer[pair.Key] = pair.Value;
        foreach (var playerIndex in _eliminatedPlayers)
            clone._eliminatedPlayers.Add(playerIndex);

        foreach (var building in _buildings)
            clone._buildings.Add(building.Clone());
        foreach (var unit in _units)
            clone._units.Add(unit.Clone());
        foreach (var stone in _gravestones)
            clone._gravestones.Add(stone.Clone());

        return clone;
    }

    /// <summary>Dynamic match state for a save file (terrain and catalog come from the map and composition).</summary>
    public MatchRuntimeSnapshot ToSnapshot(GridCell cursor)
    {
        var money = new List<int>(_playerCount);
        var turnStarts = new List<int>(_playerCount);
        var kingRehires = new List<int>(_playerCount);
        var eliminated = new List<int>();
        for (var playerIndex = 0; playerIndex < _playerCount; playerIndex++)
        {
            money.Add(_moneyByPlayer.GetValueOrDefault(playerIndex));
            turnStarts.Add(_turnStartsByPlayer.GetValueOrDefault(playerIndex));
            kingRehires.Add(_kingRehireCountByPlayer.GetValueOrDefault(playerIndex));
            if (IsPlayerEliminated(playerIndex))
                eliminated.Add(playerIndex);
        }

        return new MatchRuntimeSnapshot
        {
            PlayerCount = _playerCount,
            CurrentPlayer = CurrentPlayer,
            TurnNumber = TurnNumber,
            NextUnitId = _nextUnitId,
            UnitCap = UnitCap,
            MoneyByPlayer = money,
            TurnStartsByPlayer = turnStarts,
            KingRehireCountByPlayer = kingRehires,
            EliminatedPlayers = eliminated,
            Cursor = MatchSnapshotMapper.ToCell(cursor),
            WinnerPlayerIndex = WinnerPlayerIndex,
            VictoryReason = VictoryReason,
            Units = _units.Select(MatchSnapshotMapper.ToSnapshot).ToList(),
            Buildings = _buildings.Select(MatchSnapshotMapper.ToSnapshot).ToList(),
            Gravestones = _gravestones.Select(MatchSnapshotMapper.ToSnapshot).ToList(),
        };
    }

    /// <summary>
    /// Replaces dynamic match state from a save snapshot (terrain / catalog stay from <see cref="FromMap"/>).
    /// Does not run turn-start economy. Entity fields are mapped by <see cref="MatchSnapshotMapper"/>.
    /// </summary>
    public void HydrateFromSnapshot(MatchRuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.PlayerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(snapshot), "playerCount must be >= 1.");
        if (snapshot.MoneyByPlayer.Count != snapshot.PlayerCount
            || snapshot.TurnStartsByPlayer.Count != snapshot.PlayerCount
            || snapshot.KingRehireCountByPlayer.Count != snapshot.PlayerCount)
        {
            throw new ArgumentException("Player arrays must match playerCount.", nameof(snapshot));
        }

        _playerCount = snapshot.PlayerCount;
        _nextUnitId = Math.Max(0, snapshot.NextUnitId);
        CurrentPlayer = Math.Clamp(snapshot.CurrentPlayer, 0, snapshot.PlayerCount - 1);
        TurnNumber = Math.Max(1, snapshot.TurnNumber);
        WinnerPlayerIndex = snapshot.WinnerPlayerIndex;
        VictoryReason = snapshot.VictoryReason;
        LastAction = null;

        _moneyByPlayer.Clear();
        _turnStartsByPlayer.Clear();
        _kingRehireCountByPlayer.Clear();
        _eliminatedPlayers.Clear();
        _units.Clear();
        _buildings.Clear();
        _gravestones.Clear();

        for (var playerIndex = 0; playerIndex < snapshot.PlayerCount; playerIndex++)
        {
            _moneyByPlayer[playerIndex] = snapshot.MoneyByPlayer[playerIndex];
            _turnStartsByPlayer[playerIndex] = snapshot.TurnStartsByPlayer[playerIndex];
            _kingRehireCountByPlayer[playerIndex] = snapshot.KingRehireCountByPlayer[playerIndex];
        }

        foreach (var eliminated in snapshot.EliminatedPlayers)
        {
            if (eliminated >= 0 && eliminated < snapshot.PlayerCount)
                _eliminatedPlayers.Add(eliminated);
        }

        foreach (var building in snapshot.Buildings)
        {
            var restored = MatchSnapshotMapper.FromSnapshot(building);
            if (!_catalog.TryGetBuilding(restored.TypeId, out _))
            {
                throw new InvalidOperationException(
                    $"Saved building type '{building.TypeId}' is not in the match content catalog.");
            }

            _buildings.Add(restored);
        }

        foreach (var unit in snapshot.Units)
        {
            var restored = MatchSnapshotMapper.FromSnapshot(unit);
            if (!_catalog.TryGetUnit(restored.TypeId, out _))
            {
                throw new InvalidOperationException(
                    $"Saved unit type '{unit.TypeId}' is not in the match content catalog.");
            }

            _units.Add(restored);
        }

        foreach (var stone in snapshot.Gravestones)
            _gravestones.Add(MatchSnapshotMapper.FromSnapshot(stone));

        // Keep next id above any restored unit id.
        foreach (var unit in _units)
            _nextUnitId = Math.Max(_nextUnitId, unit.Id + 1);
    }

    public TerrainKind GetTerrain(GridCell cell) => _terrain[cell.X, cell.Y];

    public TerrainKind GetTerrain(int x, int y) => _terrain[x, y];

    public bool IsInBounds(GridCell cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    public bool IsOccupiedByUnit(GridCell cell, int? exceptUnitId = null)
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

    public bool HasGravestoneAt(GridCell cell)
    {
        foreach (var stone in _gravestones)
        {
            if (stone.Cell == cell)
                return true;
        }

        return false;
    }

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
        VictoryReason = string.IsNullOrWhiteSpace(reason) ? "victory" : reason;
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

        var useStandardDefeat = MatchConditionTypes.IsStandard(_defeatType);
        var useStandardVictory = MatchConditionTypes.IsStandard(_victoryType);
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

    /// <summary>
    /// A Confirm (click / A) on <paramref name="cell"/>: resolves it with <see cref="MatchActionResolver"/> and applies
    /// the result. Clears <see cref="LastAction"/> even when the click does nothing.
    /// </summary>
    public MatchApplyResult ConfirmAt(GridCell cell, int? selectedUnitId)
    {
        if (IsMatchOver || IsPlayerEliminated(CurrentPlayer))
            return new MatchApplyResult(false, NormalizeSelection(selectedUnitId));

        LastAction = null;
        if (selectedUnitId is int selectedId && !(TryGetUnit(selectedId, out var selected) && selected.IsActive))
            return new MatchApplyResult(false, null);

        var action = MatchActionResolver.ResolveConfirm(this, cell, selectedUnitId);
        if (action is null)
            return new MatchApplyResult(false, NormalizeSelection(selectedUnitId));

        return ApplyValidated(action, selectedUnitId);
    }

    /// <summary>Applies <paramref name="action"/> when <see cref="MatchActionRules.IsValid"/> allows it.</summary>
    public MatchApplyResult TryApply(MatchAction action, int? selectedUnitId)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!MatchActionRules.IsValid(this, action, selectedUnitId))
            return new MatchApplyResult(false, NormalizeSelection(selectedUnitId));

        return ApplyValidated(action, selectedUnitId);
    }

    /// <summary>Cancels the current activation, undoing a move made in it. Applied is false when nothing was selected.</summary>
    public MatchApplyResult ClearSelection(int? selectedUnitId)
    {
        if (selectedUnitId is null)
            return new MatchApplyResult(false, null);

        if (TryGetUnit(selectedUnitId.Value, out var unit) && unit.HasMovedThisActivation)
            UndoMove(unit);

        LastAction = null;
        return new MatchApplyResult(true, null);
    }

    public bool TryGetUnit(int unitId, out MatchUnit unit)
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

    /// <summary>An own castle occupied by an own active unit asks the player: move the unit or buy.</summary>
    public bool NeedsCastleUnitActionChooser(GridCell cell, int? selectedUnitId) =>
        selectedUnitId is null
        && IsOwnCastleAt(cell)
        && TryGetUnitAt(cell, out var unit)
        && unit.PlayerIndex == CurrentPlayer
        && unit.IsActive;

    public int ResolveRecruitCost(ContentId unitTypeId, int baseCost) =>
        MatchEconomy.ResolveRecruitCost(this, unitTypeId, baseCost);

    /// <summary>
    /// Action overlay for the selected active unit (move / attack / capture / repair / raise).
    /// After a move this turn, move cells are empty and actions are from the current cell only.
    /// </summary>
    public bool TryGetSelectedUnitActionOverlay(int? selectedUnitId, out MatchUnitActionOverlay overlay)
    {
        overlay = null!;
        if (selectedUnitId is not int unitId
            || !TryGetUnit(unitId, out var unit)
            || !unit.IsActive
            || !_catalog.TryGetUnit(unit.TypeId, out var definition))
        {
            return false;
        }

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

    /// <summary>
    /// Whether the current player already has a living unit of a <c>uniquePerPlayer</c> type
    /// (e.g. king) — shop should hide that offer.
    /// </summary>
    public bool IsUniqueUnitOwnedByCurrentPlayer(ContentId unitTypeId)
    {
        if (!_catalog.TryGetUnit(unitTypeId, out var definition))
            return false;
        if (!MatchUnitAbilities.HasAbility(definition, UnitAbilityTypes.UniquePerPlayer))
            return false;

        return _units.Any(unit => unit.PlayerIndex == CurrentPlayer && unit.TypeId == unitTypeId);
    }

    /// <summary>
    /// Best (max) attackAura value covering <paramref name="cell"/> for <paramref name="playerIndex"/>.
    /// </summary>
    public int GetAttackAuraBonus(GridCell cell, int playerIndex) =>
        MatchCombat.ResolveAttackAuraBonus(this, cell, playerIndex);

    internal void FinishUnitActivation(MatchUnit unit, MatchActionKind kind, ref int? selection)
    {
        unit.IsActive = false;
        selection = null;
        LastAction ??= new MatchPlayerAction
        {
            Kind = kind,
            PlayerIndex = CurrentPlayer,
            UnitId = unit.Id,
            Source = unit.Cell,
            Target = unit.Cell,
        };
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

    private MatchApplyResult ApplyValidated(MatchAction action, int? selectedUnitId)
    {
        var selection = selectedUnitId;
        switch (action.Kind)
        {
            case MatchActionKind.EndTurn:
                ApplyEndTurn(ref selection);
                return Applied(selection);
            case MatchActionKind.RecruitUnit:
                selection = MatchEconomy.ApplyRecruit(this, action.UnitTypeId, action.Target);
                return Applied(selection);
            case MatchActionKind.SelectUnit:
                LastAction = null;
                ApplySelect(action.UnitId, ref selection);
                return Applied(selection);
        }

        LastAction = null;
        TryGetUnit(action.UnitId, out var unit);
        _catalog.TryGetUnit(unit.TypeId, out var definition);

        switch (action.Kind)
        {
            case MatchActionKind.MoveUnit:
                ApplyMove(unit, definition, action.Target, ref selection);
                break;
            case MatchActionKind.AttackUnit:
                TryGetUnitAt(action.Target, out var defender);
                MatchCombat.ApplyAttack(this, unit, definition, defender, ref selection);
                break;
            case MatchActionKind.DestroyBuilding:
                TryGetBuildingAt(action.Target, out var target);
                MatchCombat.ApplyDestroyBuilding(this, unit, target, ref selection);
                break;
            case MatchActionKind.RaiseSkeleton:
                ApplyRaiseSkeleton(unit, action.Target, ref selection);
                break;
            case MatchActionKind.CaptureBuilding:
                ApplyCapture(unit, ref selection);
                break;
            case MatchActionKind.RepairBuilding:
                ApplyRepair(unit, ref selection);
                break;
            case MatchActionKind.WaitUnit:
                FinishUnitActivation(unit, MatchActionKind.WaitUnit, ref selection);
                break;
        }

        return Applied(selection);
    }

    private MatchApplyResult Applied(int? selection) =>
        new(true, NormalizeSelection(selection));

    private void ApplySelect(int unitId, ref int? selection)
    {
        TryGetUnit(unitId, out var unit);
        selection = unit.Id;
        LastAction = new MatchPlayerAction
        {
            Kind = MatchActionKind.SelectUnit,
            PlayerIndex = CurrentPlayer,
            UnitId = unit.Id,
            Source = unit.Cell,
            Target = unit.Cell,
        };
    }

    private void ApplyMove(MatchUnit unit, UnitDefinition definition, GridCell destination, ref int? selection)
    {
        var source = unit.Cell;
        unit.CellBeforeMove = source;
        unit.Cell = destination;
        unit.HasMovedThisActivation = true;
        LastAction = new MatchPlayerAction
        {
            Kind = MatchActionKind.MoveUnit,
            PlayerIndex = CurrentPlayer,
            UnitId = unit.Id,
            Source = source,
            Target = destination,
        };

        if (MatchUnitAbilities.HasAbility(definition, UnitAbilityTypes.MoveOrAttackExclusive))
            FinishUnitActivation(unit, MatchActionKind.MoveUnit, ref selection);
        else
            selection = unit.Id;
    }

    private void ApplyRaiseSkeleton(MatchUnit unit, GridCell stoneCell, ref int? selection)
    {
        var skeletonTypeId = MatchActionRules.RaisedUnitTypeId(unit);
        _catalog.TryGetUnit(skeletonTypeId, out var skeletonDefinition);

        _gravestones.RemoveAt(_gravestones.FindIndex(stone => stone.Cell == stoneCell));
        var spawned = SpawnUnit(
            skeletonTypeId,
            stoneCell,
            CurrentPlayer,
            skeletonDefinition.MaxHealth,
            skeletonDefinition.MaxHealth);
        spawned.IsActive = false;

        LastAction = new MatchPlayerAction
        {
            Kind = MatchActionKind.RaiseSkeleton,
            PlayerIndex = CurrentPlayer,
            UnitId = unit.Id,
            Source = unit.Cell,
            Target = stoneCell,
        };
        FinishUnitActivation(unit, MatchActionKind.RaiseSkeleton, ref selection);
    }

    private void ApplyCapture(MatchUnit unit, ref int? selection)
    {
        TryGetBuildingAt(unit.Cell, out var building);
        building.OwnerPlayerIndex = CurrentPlayer;
        LastAction = new MatchPlayerAction
        {
            Kind = MatchActionKind.CaptureBuilding,
            PlayerIndex = CurrentPlayer,
            UnitId = unit.Id,
            Source = unit.Cell,
            Target = unit.Cell,
        };
        FinishUnitActivation(unit, MatchActionKind.CaptureBuilding, ref selection);
    }

    private void ApplyRepair(MatchUnit unit, ref int? selection)
    {
        TryGetBuildingAt(unit.Cell, out var building);
        building.IsRuined = false;
        building.OwnerPlayerIndex = null;
        building.RepairedThisOwnerTurn = true;
        LastAction = new MatchPlayerAction
        {
            Kind = MatchActionKind.RepairBuilding,
            PlayerIndex = CurrentPlayer,
            UnitId = unit.Id,
            Source = unit.Cell,
            Target = unit.Cell,
        };
        FinishUnitActivation(unit, MatchActionKind.RepairBuilding, ref selection);
    }

    private void ApplyEndTurn(ref int? selection)
    {
        LastAction = null;
        selection = null;
        foreach (var unit in _units)
        {
            if (unit.PlayerIndex == CurrentPlayer)
                unit.IsActive = false;
        }

        AdvancePastEliminatedPlayers();
    }

    private void UndoMove(MatchUnit unit)
    {
        if (!unit.HasMovedThisActivation)
            return;

        if (!IsOccupiedByUnit(unit.CellBeforeMove, exceptUnitId: unit.Id))
            unit.Cell = unit.CellBeforeMove;

        unit.HasMovedThisActivation = false;
    }

    private void BeginCurrentPlayerTurn() => MatchEconomy.BeginCurrentPlayerTurn(this);

    private bool IsPlayerStandardDefeated(int playerIndex) =>
        !PlayerHasUniqueUnit(playerIndex) && !PlayerHasDefeatCountingBuilding(playerIndex);

    private bool PlayerHasUniqueUnit(int playerIndex)
    {
        foreach (var unit in _units)
        {
            if (unit.PlayerIndex != playerIndex)
                continue;
            if (!_catalog.TryGetUnit(unit.TypeId, out var definition))
                continue;
            if (MatchUnitAbilities.HasAbility(definition, UnitAbilityTypes.UniquePerPlayer))
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
            SetVictory(playerIndex, MatchConditionTypes.Standard);
            return;
        }
    }

    private void AdvancePastEliminatedPlayers()
    {
        LastAction = null;
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

    private void EnsureKnownPlayer(int playerIndex)
    {
        if (!_moneyByPlayer.ContainsKey(playerIndex))
            throw new ArgumentOutOfRangeException(nameof(playerIndex), playerIndex, "Unknown player.");
    }

}
