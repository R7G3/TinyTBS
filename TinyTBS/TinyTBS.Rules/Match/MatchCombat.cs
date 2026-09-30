using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Rules.Match;

/// <summary>
/// Attack, counterattack, destroy-building, damage/death, and attack-aura resolution.
/// Callers validate commands through <see cref="MatchActionRules"/> first.
/// </summary>
public static class MatchCombat
{
    internal static void ApplyAttack(
        MatchState match,
        MatchUnit attacker,
        UnitDefinition attackerDefinition,
        MatchUnit defender,
        ref int? selection)
    {
        var defenderDefinition = RequireUnitDefinition(match, defender);
        var range = attacker.Cell.ManhattanDistanceTo(defender.Cell);
        var damage = ComputeDamage(match, attacker, attackerDefinition, defender, defenderDefinition, range);
        var defenderDied = ApplyDamage(match, defender, defenderDefinition, damage, ref selection);
        attacker.GainExperience(defenderDied ? 2 : 1);

        var attackerDied = false;
        if (!defenderDied
            && range == 1
            && !MatchUnitAbilities.HasAbility(defenderDefinition, UnitAbilityTypes.NoCounterattack)
            && !MatchUnitAbilities.SuppressesCounterattack(attackerDefinition, range))
        {
            var counterDamage = ComputeDamage(match, defender, defenderDefinition, attacker, attackerDefinition, range);
            attackerDied = ApplyDamage(match, attacker, attackerDefinition, counterDamage, ref selection);
            if (!attackerDied)
                defender.GainExperience(counterDamage > 0 ? 1 : 2);
        }

        match.LastAction = new MatchPlayerAction
        {
            Kind = MatchActionKind.AttackUnit,
            PlayerIndex = match.CurrentPlayer,
            UnitId = attacker.Id,
            Source = attacker.Cell,
            Target = defender.Cell,
        };

        if (attackerDied)
            selection = null;
        else
            match.FinishUnitActivation(attacker, MatchActionKind.AttackUnit, ref selection);
    }

    internal static void ApplyDestroyBuilding(
        MatchState match,
        MatchUnit unit,
        MatchBuilding building,
        ref int? selection)
    {
        building.IsRuined = true;
        match.LastAction = new MatchPlayerAction
        {
            Kind = MatchActionKind.DestroyBuilding,
            PlayerIndex = match.CurrentPlayer,
            UnitId = unit.Id,
            Source = unit.Cell,
            Target = building.Cell,
        };
        match.FinishUnitActivation(unit, MatchActionKind.DestroyBuilding, ref selection);
    }

    public static int ResolveAttackAuraBonus(MatchState match, GridCell cell, int playerIndex)
    {
        var best = 0;
        foreach (var ally in match.UnitList)
        {
            if (ally.PlayerIndex != playerIndex)
                continue;
            if (!match.ContentCatalog.TryGetUnit(ally.TypeId, out var definition))
                continue;
            if (!MatchUnitAbilities.TryGetAbility(definition, UnitAbilityTypes.AttackAura, out var aura))
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

    /// <summary>
    /// Defence add from a building on <paramref name="cell"/> (never applied to attack).
    /// Intact uses the building's defence bonus; ruined uses the ruined override or 0.
    /// </summary>
    public static int ResolveBuildingDefenceBonus(MatchState match, GridCell cell)
    {
        if (!match.TryGetBuildingAt(cell, out var building))
            return 0;
        if (!match.ContentCatalog.TryGetBuilding(building.TypeId, out var definition))
            return 0;

        if (building.IsRuined)
            return definition.Ruined?.DefenceBonus ?? 0;
        return definition.DefenceBonus;
    }

    private static int ComputeDamage(
        MatchState match,
        MatchUnit attacker,
        UnitDefinition attackerDefinition,
        MatchUnit defender,
        UnitDefinition defenderDefinition,
        int range) =>
        MatchCombatFormula.ComputeDamage(
            attackerDefinition,
            attacker.Level,
            attacker.HitPoints,
            attacker.MaxHealth,
            defenderDefinition,
            MatchTerrainRules.DefenceBonus(match.GetTerrain(defender.Cell)),
            ResolveBuildingDefenceBonus(match, defender.Cell),
            range,
            ResolveAttackAuraBonus(match, attacker.Cell, attacker.PlayerIndex));

    /// <summary>Returns true when the unit died (removed, maybe leaving a gravestone).</summary>
    private static bool ApplyDamage(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        int damage,
        ref int? selection)
    {
        if (damage <= 0)
            return false;

        unit.HitPoints -= damage;
        if (unit.HitPoints > 0)
            return false;

        var cell = unit.Cell;
        var playerIndex = unit.PlayerIndex;
        if (MatchUnitAbilities.HasAbility(definition, UnitAbilityTypes.UniquePerPlayer)
            || MatchUnitAbilities.HasAbility(definition, UnitAbilityTypes.RehireCostIncrement))
        {
            match.KingRehireCounts[playerIndex] =
                match.KingRehireCounts.GetValueOrDefault(playerIndex) + 1;
        }

        match.UnitList.Remove(unit);
        if (selection == unit.Id)
            selection = null;

        if (definition.LeavesGravestone && !match.TryGetBuildingAt(cell, out _) && !match.HasGravestoneAt(cell))
        {
            var expireAt = match.TurnStarts.GetValueOrDefault(playerIndex) + 2;
            match.GravestoneList.Add(new MatchGravestone(cell, playerIndex, expireAt));
        }

        return true;
    }

    private static UnitDefinition RequireUnitDefinition(MatchState match, MatchUnit unit) =>
        match.ContentCatalog.TryGetUnit(unit.TypeId, out var definition)
            ? definition
            : throw new InvalidOperationException($"Unit type '{unit.TypeId.Full}' is not in the match content catalog.");
}
