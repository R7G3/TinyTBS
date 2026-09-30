using TinyTBS.Rules.Maps.Models;
using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Rules.Match;

/// <summary>
/// Single source of truth for which <see cref="MatchAction"/>s are legal. Click resolution, action overlays,
/// the bot's move generator and <see cref="MatchState.TryApply"/> all use these predicates.
/// </summary>
public static class MatchActionRules
{
    public static bool IsValid(MatchState state, MatchAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        if (state.IsMatchOver)
            return false;
        if (action.Kind == MatchActionKind.EndTurn)
            return true;
        if (state.IsPlayerEliminated(state.CurrentPlayer))
            return false;

        switch (action.Kind)
        {
            case MatchActionKind.SelectUnit:
                return CanSelect(state, action.UnitId);
            case MatchActionKind.RecruitUnit:
                return CanRecruit(state, action.UnitTypeId, action.Target);
        }

        if (!TryGetActingUnit(state, action.UnitId, out var unit, out var definition))
            return false;

        return action.Kind switch
        {
            MatchActionKind.MoveUnit => CanMove(state, unit, definition, action.Target),
            MatchActionKind.AttackUnit => CanAttack(state, unit, definition, action.Target),
            MatchActionKind.DestroyBuilding => CanDestroyBuilding(state, unit, definition, action.Target),
            MatchActionKind.RaiseSkeleton => CanRaiseSkeleton(state, unit, definition, action.Target),
            MatchActionKind.CaptureBuilding => action.Target == unit.Cell && CanCaptureAt(state, unit, definition, unit.Cell),
            MatchActionKind.RepairBuilding => action.Target == unit.Cell && CanRepairAt(state, unit, definition, unit.Cell),
            MatchActionKind.WaitUnit => action.Target == unit.Cell,
            _ => false,
        };
    }

    /// <summary>The selected, active unit of the current player that unit commands act through.</summary>
    public static bool TryGetActingUnit(
        MatchState state,
        int unitId,
        out MatchUnit unit,
        out UnitDefinition definition)
    {
        definition = null!;
        if (state.SelectedUnitId != unitId
            || !state.TryGetUnit(unitId, out unit)
            || !unit.IsActive
            || unit.PlayerIndex != state.CurrentPlayer)
        {
            unit = null!;
            return false;
        }

        return state.ContentCatalog.TryGetUnit(unit.TypeId, out definition);
    }

    public static bool CanSelect(MatchState state, int unitId) =>
        state.SelectedUnitId != unitId
        && state.TryGetUnit(unitId, out var unit)
        && unit.PlayerIndex == state.CurrentPlayer
        && unit.IsActive;

    public static bool CanMove(MatchState state, MatchUnit unit, UnitDefinition definition, GridCell destination) =>
        !unit.HasMovedThisActivation
        && destination != unit.Cell
        && !state.IsOccupiedByUnit(destination, exceptUnitId: unit.Id)
        && MatchPathfinder.CanReach(state, unit, destination, definition.MovementClass, definition.Speed, unit.Id);

    public static bool CanAttack(MatchState state, MatchUnit unit, UnitDefinition definition, GridCell targetCell) =>
        !IsAttackLockedAfterMove(unit, definition)
        && state.TryGetUnitAt(targetCell, out var target)
        && IsAttackTargetFrom(state, unit, definition, unit.Cell, target);

    public static bool CanDestroyBuilding(MatchState state, MatchUnit unit, UnitDefinition definition, GridCell targetCell) =>
        !IsAttackLockedAfterMove(unit, definition)
        && state.TryGetBuildingAt(targetCell, out var building)
        && IsDestroyTargetFrom(state, unit, definition, unit.Cell, building);

    public static bool CanRaiseSkeleton(MatchState state, MatchUnit unit, UnitDefinition definition, GridCell targetCell) =>
        IsRaiseTargetFrom(state, unit, definition, unit.Cell, targetCell);

    /// <summary><c>moveOrAttackExclusive</c> units (e.g. catapult) cannot strike after moving this activation.</summary>
    public static bool IsAttackLockedAfterMove(MatchUnit unit, UnitDefinition definition) =>
        unit.HasMovedThisActivation
        && MatchUnitAbilities.HasAbility(definition, UnitAbilityTypes.MoveOrAttackExclusive);

    /// <summary>Whether <paramref name="unit"/> standing on <paramref name="fromCell"/> could attack <paramref name="target"/>.</summary>
    public static bool IsAttackTargetFrom(
        MatchState state,
        MatchUnit unit,
        UnitDefinition definition,
        GridCell fromCell,
        MatchUnit target)
    {
        if (target.Id == unit.Id || target.PlayerIndex == unit.PlayerIndex)
            return false;
        if (!IsInAttackRange(definition, fromCell, target.Cell))
            return false;
        return state.ContentCatalog.TryGetUnit(target.TypeId, out _);
    }

    /// <summary>Whether <paramref name="unit"/> standing on <paramref name="fromCell"/> could destroy <paramref name="building"/>.</summary>
    public static bool IsDestroyTargetFrom(
        MatchState state,
        MatchUnit unit,
        UnitDefinition definition,
        GridCell fromCell,
        MatchBuilding building)
    {
        if (building.IsRuined)
            return false;
        if (!MatchUnitAbilities.TryGetAbility(definition, UnitAbilityTypes.DestroyBuilding, out var destroyAbility))
            return false;
        if (!state.ContentCatalog.TryGetBuilding(building.TypeId, out var buildingDefinition)
            || !buildingDefinition.Destroyable)
        {
            return false;
        }

        if (!MatchUnitAbilities.TagsIntersect(destroyAbility.Tags, buildingDefinition.Tags))
            return false;
        if (state.IsOccupiedByUnit(building.Cell, exceptUnitId: unit.Id))
            return false;
        return IsInAttackRange(definition, fromCell, building.Cell);
    }

    /// <summary>Whether <paramref name="unit"/> standing on <paramref name="fromCell"/> could raise a skeleton on <paramref name="stoneCell"/>.</summary>
    public static bool IsRaiseTargetFrom(
        MatchState state,
        MatchUnit unit,
        UnitDefinition definition,
        GridCell fromCell,
        GridCell stoneCell)
    {
        if (!MatchUnitAbilities.HasAbility(definition, UnitAbilityTypes.RaiseSkeleton))
            return false;
        if (fromCell.ManhattanDistanceTo(stoneCell) != 1)
            return false;
        if (state.IsOccupiedByUnit(stoneCell, exceptUnitId: unit.Id))
            return false;
        if (state.CountUnitsForPlayer(unit.PlayerIndex) >= state.UnitCap)
            return false;
        if (!state.HasGravestoneAt(stoneCell))
            return false;
        return state.ContentCatalog.TryGetUnit(RaisedUnitTypeId(unit), out _);
    }

    public static bool CanCaptureAt(MatchState state, MatchUnit unit, UnitDefinition definition, GridCell cell) =>
        state.TryGetBuildingAt(cell, out var building)
        && !building.IsRuined
        && !building.RepairedThisOwnerTurn
        && building.OwnerPlayerIndex != unit.PlayerIndex
        && state.ContentCatalog.TryGetBuilding(building.TypeId, out var buildingDefinition)
        && MatchUnitAbilities.IsCapturable(building, buildingDefinition)
        && MatchUnitAbilities.CanCapture(definition, buildingDefinition);

    public static bool CanRepairAt(MatchState state, MatchUnit unit, UnitDefinition definition, GridCell cell) =>
        state.TryGetBuildingAt(cell, out var building)
        && building.IsRuined
        && state.ContentCatalog.TryGetBuilding(building.TypeId, out var buildingDefinition)
        && buildingDefinition.Repairable
        && MatchUnitAbilities.CanRepair(definition, buildingDefinition);

    /// <summary>Recruit onto an own intact recruit building if gold (with rehire surcharge), cap and uniqueness allow.</summary>
    public static bool CanRecruit(MatchState state, ContentId unitTypeId, GridCell castleCell)
    {
        if (!state.ContentCatalog.TryGetShopOffer(unitTypeId, out var offer))
            return false;
        if (!state.ContentCatalog.TryGetUnit(unitTypeId, out var definition) || definition.MaxHealth <= 0)
            return false;
        if (!state.IsOwnCastleAt(castleCell) || state.IsOccupiedByUnit(castleCell))
            return false;
        if (state.CountUnitsForPlayer(state.CurrentPlayer) >= state.UnitCap)
            return false;
        if (state.IsUniqueUnitOwnedByCurrentPlayer(unitTypeId))
            return false;
        return state.GetMoney(state.CurrentPlayer) >= MatchEconomy.ResolveRecruitCost(state, unitTypeId, offer.Cost);
    }

    public static ContentId RaisedUnitTypeId(MatchUnit raiser) =>
        new(raiser.TypeId.Namespace, UnitAbilityTypes.RaisedUnitLocalId);

    private static bool IsInAttackRange(UnitDefinition definition, GridCell fromCell, GridCell targetCell)
    {
        var range = fromCell.ManhattanDistanceTo(targetCell);
        return range >= definition.AttackRangeMin && range <= definition.AttackRangeMax;
    }
}
