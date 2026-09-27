using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// GDD damage formula (COMBAT.md): floating intermediates, one Floor at the end.
/// Does not apply combat — only computes damage for an attack or counterattack.
/// </summary>
public static class MatchCombatFormula
{
    public static int LevelStatPercent(int level) =>
        level switch
        {
            >= 3 => 30,
            2 => 20,
            1 => 10,
            _ => 0,
        };

    public static int ComputeDamage(
        UnitDefinition attacker,
        int attackerLevel,
        int attackerHitPoints,
        UnitDefinition defender,
        int defenderLevel,
        int terrainDefenceBonus,
        int buildingDefenceBonus,
        int manhattanRange,
        int attackAuraBonus = 0)
    {
        var levelAtkAdd = attacker.Attack * (LevelStatPercent(attackerLevel) / 100.0);
        var levelDefAdd = defender.Defence * (LevelStatPercent(defenderLevel) / 100.0);

        var atk = attacker.Attack + levelAtkAdd + attackAuraBonus;
        var def = defender.Defence + levelDefAdd + terrainDefenceBonus + buildingDefenceBonus;
        var diff = atk - def;
        var special = ResolveSpecial(attacker, defender, manhattanRange);
        var raw = Math.Floor(diff * special * (attackerHitPoints / 10.0));
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
