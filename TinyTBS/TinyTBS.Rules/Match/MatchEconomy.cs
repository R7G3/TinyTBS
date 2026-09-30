using TinyTBS.Rules.Buildings.Models;
using TinyTBS.Rules.Maps.Models;
using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Rules.Match;

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
            if (!match.ContentCatalog.TryGetBuilding(building.TypeId, out var definition))
                continue;

            var income = ResolveIncome(definition, building.IsRuined);
            if (income > 0)
                total += income;
        }

        return total;
    }

    public static int ResolveRecruitCost(MatchState match, ContentId unitTypeId, int baseCost)
    {
        if (!match.ContentCatalog.TryGetUnit(unitTypeId, out var definition))
            return baseCost;

        if (MatchUnitAbilities.TryGetAbility(definition, UnitAbilityTypes.RehireCostIncrement, out var rehire)
            && rehire.Amount is int increment)
        {
            return baseCost + match.KingRehireCounts[match.CurrentPlayer] * increment;
        }

        return baseCost;
    }

    /// <summary>Applies a recruit already validated by <see cref="MatchActionRules.CanRecruit"/>.</summary>
    internal static int ApplyRecruit(MatchState match, ContentId unitTypeId, GridCell castleCell)
    {
        match.ContentCatalog.TryGetShopOffer(unitTypeId, out var offer);
        match.ContentCatalog.TryGetUnit(unitTypeId, out var definition);

        match.AddMoney(match.CurrentPlayer, -ResolveRecruitCost(match, unitTypeId, offer.Cost));
        var spawned = match.SpawnUnit(unitTypeId, castleCell, match.CurrentPlayer, definition.MaxHealth, definition.MaxHealth);
        spawned.IsActive = true;
        match.LastAction = new MatchPlayerAction
        {
            Kind = MatchActionKind.RecruitUnit,
            PlayerIndex = match.CurrentPlayer,
            UnitId = spawned.Id,
            Source = castleCell,
            Target = castleCell,
        };
        return spawned.Id;
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
            if (!match.ContentCatalog.TryGetBuilding(building.TypeId, out var definition))
                continue;

            var income = ResolveIncome(definition, building.IsRuined);
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

    private static int ResolveIncome(BuildingDefinition definition, bool isRuined) =>
        isRuined ? definition.Ruined?.Income ?? 0 : definition.Income;

    private static int ResolveHealAmount(BuildingDefinition definition, bool isRuined)
    {
        if (isRuined)
            return definition.Ruined?.Heal?.Amount ?? 0;
        return definition.Heal?.Amount ?? 0;
    }
}
