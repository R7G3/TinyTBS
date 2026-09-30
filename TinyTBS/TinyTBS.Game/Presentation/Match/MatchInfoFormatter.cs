using System.Text;
using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>Builds compact corner text and per-section detail texts for the cursor cell.</summary>
public static class MatchInfoFormatter
{
    public static string FormatCompact(MatchState match, MatchContentCatalog contentCatalog, GridCell cell)
    {
        ArgumentNullException.ThrowIfNull(contentCatalog);

        var terrain = match.GetTerrain(cell);
        var builder = new StringBuilder();
        builder.Append(TerrainLabel(terrain));
        builder.Append(" · ");
        builder.Append(TerrainDefenceShort(terrain));

        if (match.TryGetBuildingAt(cell, out var building))
        {
            builder.AppendLine();
            builder.Append(contentCatalog.DisplayName(building.TypeId));
            if (building.IsRuined)
                builder.Append(" (ruined)");
            builder.Append(" · ");
            builder.Append(OwnerShort(building.OwnerPlayerIndex));
        }

        if (match.TryGetUnitAt(cell, out var unit))
        {
            builder.AppendLine();
            builder.Append(contentCatalog.DisplayName(unit.TypeId));
            builder.Append(" · P");
            builder.Append(unit.PlayerIndex + 1);
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatTerrainDetail(MatchState match, GridCell cell)
    {
        var terrain = match.GetTerrain(cell);
        return $"Terrain{Environment.NewLine}"
            + $"{TerrainLabel(terrain)}{Environment.NewLine}"
            + TerrainModifiers(terrain);
    }

    public static string FormatBuildingDetail(MatchState match, MatchContentCatalog contentCatalog, GridCell cell)
    {
        ArgumentNullException.ThrowIfNull(contentCatalog);

        if (!match.TryGetBuildingAt(cell, out var building))
            return string.Empty;

        if (!contentCatalog.TryGetBuilding(building.TypeId, out var definition))
            return string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine("Building");
        builder.AppendLine(contentCatalog.DisplayName(definition));
        builder.AppendLine($"Owner: {OwnerLabel(building.OwnerPlayerIndex)}");
        if (building.IsRuined)
            builder.AppendLine("State: ruined");

        AppendBuildingEconomy(builder, definition, building.IsRuined);
        return builder.ToString();
    }

    public static string FormatUnitDetail(MatchState match, MatchContentCatalog contentCatalog, GridCell cell)
    {
        ArgumentNullException.ThrowIfNull(contentCatalog);

        if (!match.TryGetUnitAt(cell, out var unit))
            return string.Empty;

        if (!contentCatalog.TryGetUnit(unit.TypeId, out var definition))
            return string.Empty;

        var status = unit.PlayerIndex == match.CurrentPlayer ? "yours" : "enemy";
        var name = contentCatalog.DisplayName(definition);
        var levelAttackBonus = unit.AttackBonusFromLevel;
        var auraBonus = match.GetAttackAuraBonus(unit.Cell, unit.PlayerIndex);
        var terrainDefenceBonus = MatchTerrainRules.DefenceBonus(match.GetTerrain(unit.Cell));
        var buildingDefenceBonus = MatchCombat.ResolveBuildingDefenceBonus(match, unit.Cell);

        var attackParts = new List<string> { definition.Attack.ToString() };
        if (levelAttackBonus > 0)
            attackParts.Add($"+{levelAttackBonus}");
        if (auraBonus > 0)
            attackParts.Add($"+{auraBonus}");
        var attackText = attackParts.Count == 1
            ? $"Atk: {definition.Attack}"
            : $"Atk: {string.Join(" ", attackParts)}";

        var defenceParts = new List<string> { definition.Defence.ToString() };
        if (terrainDefenceBonus > 0)
            defenceParts.Add($"+{terrainDefenceBonus}");
        if (buildingDefenceBonus > 0)
            defenceParts.Add($"+{buildingDefenceBonus}");
        var defenceText = defenceParts.Count == 1
            ? $"Def: {definition.Defence}"
            : $"Def: {string.Join(" ", defenceParts)}";

        var levelBonusText = $"Level: {unit.Level} (+{levelAttackBonus} Atk)";

        var rangeText = definition.AttackRangeMin == definition.AttackRangeMax
            ? definition.AttackRangeMin.ToString()
            : $"{definition.AttackRangeMin}–{definition.AttackRangeMax}";

        return $"Unit{Environment.NewLine}"
            + $"{name}{Environment.NewLine}"
            + $"Owner: Player {unit.PlayerIndex + 1}{Environment.NewLine}"
            + $"Status: {status}{Environment.NewLine}"
            + $"HP: {unit.HitPoints}/{unit.MaxHealth}{Environment.NewLine}"
            + $"{levelBonusText}{Environment.NewLine}"
            + $"{attackText}  {defenceText}{Environment.NewLine}"
            + $"Range {rangeText}  Speed {definition.Speed}";
    }

    private static void AppendBuildingEconomy(
        StringBuilder builder,
        BuildingDefinition definition,
        bool isRuined)
    {
        if (isRuined && definition.Ruined is { } ruined)
        {
            builder.AppendLine($"Income: {ruined.Income}g / turn");
            if (ruined.Heal is { } ruinedHeal)
            {
                builder.AppendLine(
                    ruinedHeal.Amount <= 0 || string.Equals(ruinedHeal.Scope, "none", StringComparison.OrdinalIgnoreCase)
                        ? "Heal: none"
                        : $"Heal: +{ruinedHeal.Amount} HP / turn ({ruinedHeal.Scope})");
            }
            else
            {
                builder.AppendLine("Heal: none");
            }

            builder.Append($"Defence bonus: +{ruined.DefenceBonus}");
            return;
        }

        builder.AppendLine($"Income: {definition.Income}g / turn when owned (from turn 2)");
        if (definition.Heal is { } heal)
        {
            builder.AppendLine(
                heal.Amount <= 0 || string.Equals(heal.Scope, "none", StringComparison.OrdinalIgnoreCase)
                    ? "Heal: none"
                    : $"Heal: +{heal.Amount} HP / turn ({heal.Scope})");
        }

        builder.Append($"Defence bonus: +{definition.DefenceBonus}");
    }

    private static string OwnerShort(int? ownerPlayerIndex) =>
        ownerPlayerIndex is int index ? PlayerDisplayNames.Number(index) : "N";

    private static string OwnerLabel(int? ownerPlayerIndex) =>
        PlayerDisplayNames.EditorOwner(ownerPlayerIndex);

    private static string TerrainLabel(TerrainKind terrain) => terrain switch
    {
        TerrainKind.Water => "Water",
        TerrainKind.Road => "Road",
        TerrainKind.Mountain => "Mountain",
        TerrainKind.Bridge => "Bridge",
        TerrainKind.Forest => "Forest",
        _ => "Grass",
    };

    private static string TerrainDefenceShort(TerrainKind terrain) => terrain switch
    {
        TerrainKind.Road or TerrainKind.Bridge or TerrainKind.Water => "Def +0",
        TerrainKind.Mountain => "Def +15",
        TerrainKind.Forest => "Def +10",
        _ => "Def +5",
    };

    private static string TerrainModifiers(TerrainKind terrain) => terrain switch
    {
        TerrainKind.Road => "Defence +0 · Move cost 1",
        TerrainKind.Bridge => "Defence +0 · Move cost 1",
        TerrainKind.Mountain => "Defence +15 · Move cost 3",
        TerrainKind.Forest => "Defence +10 · Move cost 2",
        TerrainKind.Water => "Defence +0 · Move cost 4",
        _ => "Defence +5 · Move cost 1",
    };
}
