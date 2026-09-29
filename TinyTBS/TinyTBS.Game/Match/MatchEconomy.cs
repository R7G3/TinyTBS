using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match;

/// <summary>Turn-start income/heal, gravestone expiry, and castle recruit.</summary>
public static class MatchEconomy
{
    public static void BeginCurrentPlayerTurn(MatchState match)
    {
        match.TurnStarts[match.CurrentPlayer]++;
        ExpireGravestonesForCurrentPlayer(match);

        foreach (var building in match.BuildingList)
            building.RepairedThisOwnerTurn = false;

        foreach (var unit in match.UnitList)
        {
            if (unit.PlayerIndex != match.CurrentPlayer)
                continue;
            unit.IsActive = true;
            unit.HasMovedThisActivation = false;
            unit.CellBeforeMove = unit.Cell;
        }

        if (match.TurnStarts[match.CurrentPlayer] >= 2)
            ApplyIncomeAndHeal(match, match.CurrentPlayer);
    }

    /// <summary>
    /// Gold the player would receive from currently owned buildings (same rules as turn-start income).
    /// </summary>
    public static int CalculateOwnedBuildingIncome(MatchState match, int playerIndex)
    {
        ArgumentNullException.ThrowIfNull(match);

        var total = 0;
        foreach (var building in match.BuildingList)
        {
            if (building.OwnerPlayerIndex != playerIndex)
                continue;
            if (!match.Catalog.TryGetBuilding(building.TypeId, out var definition))
                continue;

            var income = building.IsRuined
                ? definition.Ruined?.Income ?? 0
                : definition.Income;
            if (income > 0)
                total += income;
        }

        return total;
    }

    public static bool TryRecruitAtCastle(
        MatchState match,
        ContentId unitTypeId,
        int baseCost,
        int maxHealth,
        GridCell castleCell)
    {
        if (match.IsMatchOver || match.IsPlayerEliminated(match.CurrentPlayer))
            return false;
        if (!match.IsOwnCastleAt(castleCell))
            return false;
        if (match.IsOccupiedByUnitPublic(castleCell))
            return false;
        if (match.CountUnitsForPlayer(match.CurrentPlayer) >= match.UnitCap)
            return false;
        if (maxHealth <= 0)
            return false;
        if (!match.Catalog.TryGetUnit(unitTypeId, out var definition))
            return false;

        if (MatchUnitAbilities.HasAbility(definition, "uniquePerPlayer")
            && match.UnitList.Any(unit =>
                unit.PlayerIndex == match.CurrentPlayer && unit.TypeId == unitTypeId))
        {
            return false;
        }

        var cost = ResolveRecruitCost(match, unitTypeId, baseCost);
        if (match.GetMoney(match.CurrentPlayer) < cost)
            return false;

        match.AddMoney(match.CurrentPlayer, -cost);
        var spawned = match.SpawnUnit(unitTypeId, castleCell, match.CurrentPlayer, maxHealth, maxHealth);
        spawned.IsActive = true;
        match.LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.RecruitUnit,
            PlayerIndex = match.CurrentPlayer,
            UnitId = spawned.Id,
            Source = castleCell,
            Target = castleCell,
        };
        match.SelectedUnitId = spawned.Id;
        return true;
    }

    public static int ResolveRecruitCost(MatchState match, ContentId unitTypeId, int baseCost)
    {
        if (!match.Catalog.TryGetUnit(unitTypeId, out var definition))
            return baseCost;

        if (MatchUnitAbilities.TryGetAbility(definition, "rehireCostIncrement", out var rehire)
            && rehire.Amount is int increment)
        {
            return baseCost + match.KingRehireCounts[match.CurrentPlayer] * increment;
        }

        return baseCost;
    }

    private static void ExpireGravestonesForCurrentPlayer(MatchState match)
    {
        var turnStarts = match.TurnStarts[match.CurrentPlayer];
        match.GravestoneList.RemoveAll(stone =>
            stone.SourcePlayerIndex == match.CurrentPlayer
            && turnStarts >= stone.ExpiresWhenTurnStartsReaches);
    }

    private static void ApplyIncomeAndHeal(MatchState match, int playerIndex)
    {
        foreach (var building in match.BuildingList)
        {
            if (building.OwnerPlayerIndex != playerIndex)
                continue;
            if (!match.Catalog.TryGetBuilding(building.TypeId, out var definition))
                continue;

            var income = building.IsRuined
                ? definition.Ruined?.Income ?? 0
                : definition.Income;
            if (income > 0)
                match.AddMoney(playerIndex, income);

            var healAmount = ResolveHealAmount(definition, building.IsRuined);
            if (healAmount <= 0)
                continue;

            if (!match.TryGetUnitAt(building.Cell, out var occupant) || occupant.PlayerIndex != playerIndex)
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
}
