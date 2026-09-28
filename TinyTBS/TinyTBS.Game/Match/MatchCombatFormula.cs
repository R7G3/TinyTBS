using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// GDD damage formula (COMBAT.md): floating intermediates, one Floor at the end.
/// Does not apply combat — only computes damage for an attack or counterattack.
/// </summary>
public static class MatchCombatFormula
{
    /// <summary>Flat attack add from unit level (+2 per level above 0).</summary>
    public static int AttackBonusFromLevel(int level) =>
        Math.Max(0, level) * 2;

    public static int ComputeDamage(
        UnitDefinition attacker,
        int attackerLevel,
        int attackerHitPoints,
        int attackerMaxHealth,
        UnitDefinition defender,
        int terrainDefenceBonus,
        int buildingDefenceBonus,
        int manhattanRange,
        int attackAuraBonus = 0)
    {
        var levelAtkAdd = AttackBonusFromLevel(attackerLevel);

        // Building bonuses apply only to defence (buildingDefenceBonus), never to attack.
        var atk = attacker.Attack + levelAtkAdd + attackAuraBonus;
        var def = defender.Defence + terrainDefenceBonus + buildingDefenceBonus;
        var diff = atk - def;
        var special = ResolveSpecial(attacker, defender, manhattanRange);
        var healthFraction = attackerMaxHealth > 0
            ? attackerHitPoints / (double)attackerMaxHealth
            : 0.0;
        var raw = Math.Floor(diff * special * healthFraction);
        return raw < 0 ? 0 : (int)raw;
    }

    public static double ResolveSpecial(
        UnitDefinition attacker,
        UnitDefinition defender,
        int manhattanRange)
    {
        foreach (var entry in attacker.SpecialCoefficients)
        {
            var when = entry.When;
            if (when.IsDefault)
                return entry.Multiply;

            if (!string.IsNullOrWhiteSpace(when.TargetHasTag)
                && defender.Tags.Any(tag =>
                    string.Equals(tag, when.TargetHasTag, StringComparison.OrdinalIgnoreCase)))
            {
                return entry.Multiply;
            }

            if (when.ManhattanRange is int requiredRange && requiredRange == manhattanRange)
                return entry.Multiply;
        }

        return 1.0;
    }
}
