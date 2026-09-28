using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Attack, counterattack, destroy-building, damage/death, and attack-aura resolution.
/// </summary>
public static class MatchCombat
{
    public static bool TryAttack(
        MatchState match,
        MatchUnit attacker,
        UnitDefinition attackerDefinition,
        MatchUnit defender)
    {
        if (MatchUnitAbilities.HasAbility(attackerDefinition, "moveOrAttackExclusive")
            && attacker.HasMovedThisActivation)
            return false;

        var range = attacker.Cell.ManhattanDistanceTo(defender.Cell);
        if (range < attackerDefinition.AttackRangeMin || range > attackerDefinition.AttackRangeMax)
            return false;
        if (!match.Catalog.TryGetUnit(defender.TypeId, out var defenderDefinition))
            return false;

        var terrainDef = MatchTerrainRules.DefenceBonus(match.GetTerrain(defender.Cell));
        var buildingDef = ResolveBuildingDefenceBonus(match, defender.Cell);
        var attackAura = ResolveAttackAuraBonus(match, attacker.Cell, attacker.PlayerIndex);
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

        ApplyDamage(match, defender, defenderDefinition, damage);
        var defenderDied = !match.UnitList.Contains(defender);

        if (!defenderDied)
            attacker.GainExperience(1);
        else
            attacker.GainExperience(2);

        if (!defenderDied
            && range == 1
            && !MatchUnitAbilities.HasAbility(defenderDefinition, "noCounterattack")
            && !MatchUnitAbilities.SuppressesCounterattack(attackerDefinition, range))
        {
            var counterTerrain = MatchTerrainRules.DefenceBonus(match.GetTerrain(attacker.Cell));
            var counterBuilding = ResolveBuildingDefenceBonus(match, attacker.Cell);
            var counterAura = ResolveAttackAuraBonus(match, defender.Cell, defender.PlayerIndex);
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
            ApplyDamage(match, attacker, attackerDefinition, counterDamage);
            if (match.UnitList.Contains(attacker))
                defender.GainExperience(counterDamage > 0 && attacker.HitPoints > 0 ? 1 : 2);
        }

        match.LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.AttackUnit,
            PlayerIndex = match.CurrentPlayer,
            UnitId = attacker.Id,
            Source = attacker.Cell,
            Target = defender.Cell,
        };

        if (match.UnitList.Contains(attacker))
            match.FinishUnitActivation(attacker, MatchPlayerActionKind.AttackUnit);
        else
            match.SelectedUnitId = null;

        return true;
    }

    public static bool TryDestroyBuildingAtCursor(
        MatchState match,
        MatchUnit unit,
        UnitDefinition unitDefinition)
    {
        if (MatchUnitAbilities.HasAbility(unitDefinition, "moveOrAttackExclusive")
            && unit.HasMovedThisActivation)
            return false;
        if (!MatchUnitAbilities.TryGetAbility(unitDefinition, "destroyBuilding", out var destroyAbility))
            return false;
        if (!match.TryGetBuildingAt(match.Cursor, out var building) || building.IsRuined)
            return false;
        if (!match.Catalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
            return false;
        if (!buildingDefinition.Destroyable)
            return false;
        if (!MatchUnitAbilities.TagsIntersect(destroyAbility.Tags, buildingDefinition.Tags))
            return false;
        if (match.IsOccupiedByUnitPublic(building.Cell))
            return false;

        var range = unit.Cell.ManhattanDistanceTo(building.Cell);
        if (range < unitDefinition.AttackRangeMin || range > unitDefinition.AttackRangeMax)
            return false;

        building.IsRuined = true;
        match.LastAction = new MatchPlayerAction
        {
            Kind = MatchPlayerActionKind.DestroyBuilding,
            PlayerIndex = match.CurrentPlayer,
            UnitId = unit.Id,
            Source = unit.Cell,
            Target = building.Cell,
        };
        match.FinishUnitActivation(unit, MatchPlayerActionKind.DestroyBuilding);
        return true;
    }

    public static int ResolveAttackAuraBonus(MatchState match, GridCell cell, int playerIndex)
    {
        var best = 0;
        foreach (var ally in match.UnitList)
        {
            if (ally.PlayerIndex != playerIndex)
                continue;
            if (!match.Catalog.TryGetUnit(ally.TypeId, out var definition))
                continue;
            if (!MatchUnitAbilities.TryGetAbility(definition, "attackAura", out var aura))
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

    public static void ApplyDamage(
        MatchState match,
        MatchUnit unit,
        UnitDefinition definition,
        int damage)
    {
        if (damage <= 0)
            return;

        unit.HitPoints -= damage;
        if (unit.HitPoints > 0)
            return;

        var cell = unit.Cell;
        var playerIndex = unit.PlayerIndex;
        if (MatchUnitAbilities.HasAbility(definition, "uniquePerPlayer")
            || MatchUnitAbilities.HasAbility(definition, "rehireCostIncrement"))
        {
            match.KingRehireCounts[playerIndex] =
                match.KingRehireCounts.GetValueOrDefault(playerIndex) + 1;
        }

        match.UnitList.Remove(unit);
        if (match.SelectedUnitId == unit.Id)
            match.SelectedUnitId = null;

        if (definition.LeavesGravestone && !match.TryGetBuildingAt(cell, out _))
        {
            if (!match.GravestoneList.Any(stone => stone.Cell == cell))
            {
                var expireAt = match.TurnStarts.GetValueOrDefault(playerIndex) + 2;
                match.GravestoneList.Add(new MatchGravestone(cell, playerIndex, expireAt));
            }
        }
    }

    private static int ResolveBuildingDefenceBonus(MatchState match, GridCell cell)
    {
        if (!match.TryGetBuildingAt(cell, out var building))
            return 0;
        if (!match.Catalog.TryGetBuilding(building.TypeId, out var definition))
            return 0;

        if (building.IsRuined)
            return definition.Ruined?.DefenceBonus ?? 0;
        return definition.DefenceBonus;
    }
}
