using System.Text;

namespace TinyTBS.Game.Match;

/// <summary>Builds compact corner text and per-section detail texts for the cursor cell.</summary>
public static class MatchInfoFormatter
{
    public static string FormatCompact(MatchState match)
    {
        var cell = match.Cursor;
        var terrain = match.GetTerrain(cell);
        var builder = new StringBuilder();
        builder.Append(TerrainLabel(terrain));
        builder.Append(" · ");
        builder.Append(TerrainDefenceShort(terrain));

        if (match.TryGetBuildingAt(cell, out var building))
        {
            builder.AppendLine();
            builder.Append(building.Kind);
            builder.Append(" · ");
            builder.Append(OwnerShort(building.OwnerPlayerIndex));
        }

        if (match.TryGetUnitAt(cell, out var unit))
        {
            builder.AppendLine();
            builder.Append(unit.Kind);
            builder.Append(" · P");
            builder.Append(unit.PlayerIndex + 1);
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatDetailHeader(MatchState match) =>
        $"Cell {match.Cursor.X},{match.Cursor.Y}";

    public static string FormatTerrainDetail(MatchState match)
    {
        var terrain = match.GetTerrain(match.Cursor);
        return $"Terrain{Environment.NewLine}"
            + $"{TerrainLabel(terrain)}{Environment.NewLine}"
            + TerrainModifiers(terrain);
    }

    public static string FormatBuildingDetail(MatchState match)
    {
        if (!match.TryGetBuildingAt(match.Cursor, out var building))
            return string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine("Building");
        builder.AppendLine($"{building.Kind}");
        builder.AppendLine($"Owner: {OwnerLabel(building.OwnerPlayerIndex)}");
        if (building.Kind == BuildingKind.Castle)
        {
            builder.AppendLine("Income: 50g / turn (from turn 2)");
            builder.AppendLine("Heal: +20 HP / turn (allied)");
            builder.Append("Defence bonus: +15");
        }
        else
        {
            builder.AppendLine("Income: 30g / turn when owned (from turn 2)");
            builder.AppendLine("Heal: +20 HP / turn (allied, intact)");
            builder.Append("Defence bonus: +15 (intact)");
        }

        return builder.ToString();
    }

    public static string FormatUnitDetail(MatchState match)
    {
        if (!match.TryGetUnitAt(match.Cursor, out var unit))
            return string.Empty;

        var status = unit.PlayerIndex == match.CurrentPlayer ? "yours" : "enemy";
        return $"Unit{Environment.NewLine}"
            + $"{unit.Kind}{Environment.NewLine}"
            + $"Owner: Player {unit.PlayerIndex + 1}{Environment.NewLine}"
            + $"Status: {status}{Environment.NewLine}"
            + "Stats: see unit data (demo)";
    }

    private static string OwnerShort(int? ownerPlayerIndex) =>
        ownerPlayerIndex is int index ? $"P{index + 1}" : "N";

    private static string OwnerLabel(int? ownerPlayerIndex) =>
        ownerPlayerIndex is int index ? $"Player {index + 1}" : "Neutral";

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
