using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>Shared ability / tag checks for units and buildings.</summary>
public static class MatchUnitAbilities
{
    public static bool HasAbility(UnitDefinition definition, string type) =>
        definition.Abilities.Any(ability =>
            string.Equals(ability.Type, type, StringComparison.OrdinalIgnoreCase));

    public static bool TryGetAbility(
        UnitDefinition definition,
        string type,
        out UnitAbilityDefinition ability)
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

    public static bool TagsIntersect(IReadOnlyList<string> left, IReadOnlyList<string> right)
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

    public static bool IsCapturable(MatchBuilding building, BuildingDefinition definition)
    {
        if (building.IsRuined)
            return definition.Ruined?.Capturable ?? false;
        return definition.Capturable;
    }

    public static bool CanCapture(UnitDefinition unit, BuildingDefinition building) =>
        unit.Abilities.Any(ability =>
            string.Equals(ability.Type, "captureBuilding", StringComparison.OrdinalIgnoreCase)
            && ability.Tags.Any(tag =>
                building.Tags.Any(buildingTag =>
                    string.Equals(tag, buildingTag, StringComparison.OrdinalIgnoreCase))));

    public static bool CanRepair(UnitDefinition unit, BuildingDefinition building) =>
        unit.Abilities.Any(ability =>
            string.Equals(ability.Type, "repairBuilding", StringComparison.OrdinalIgnoreCase)
            && ability.Tags.Any(tag =>
                building.Tags.Any(buildingTag =>
                    string.Equals(tag, buildingTag, StringComparison.OrdinalIgnoreCase))));

    public static bool SuppressesCounterattack(UnitDefinition attacker, int range) =>
        attacker.Abilities.Any(ability =>
            string.Equals(ability.Type, "noCounterattackWhenRangeAtLeast", StringComparison.OrdinalIgnoreCase)
            && ability.MinRange is int min
            && range >= min);
}
