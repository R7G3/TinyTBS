using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Maps;

/// <summary>
/// Resolves vanilla/* logical ids to match enums until full module-driven catalogs replace this bridge.
/// </summary>
public static class VanillaContentIds
{
    public static TerrainKind ParseTerrain(string typeId)
    {
        return typeId.Trim().ToLowerInvariant() switch
        {
            "grass" => TerrainKind.Grass,
            "water" => TerrainKind.Water,
            "road" => TerrainKind.Road,
            "mountain" => TerrainKind.Mountain,
            "bridge" => TerrainKind.Bridge,
            "forest" => TerrainKind.Forest,
            _ => throw new MapLoadException($"Unknown terrain type '{typeId}'."),
        };
    }

    public static BuildingKind ParseBuilding(ContentId contentId)
    {
        if (!IsVanilla(contentId))
            throw Unresolved(contentId, "building");

        return contentId.LocalId.ToLowerInvariant() switch
        {
            "castle" => BuildingKind.Castle,
            "village" => BuildingKind.Village,
            _ => throw Unresolved(contentId, "building"),
        };
    }

    public static UnitKind ParseUnit(ContentId contentId)
    {
        if (!IsVanilla(contentId))
            throw Unresolved(contentId, "unit");

        return contentId.LocalId.ToLowerInvariant() switch
        {
            "king" => UnitKind.King,
            "swordsman" => UnitKind.Swordsman,
            "archer" => UnitKind.Archer,
            "lizard" => UnitKind.Lizard,
            "witch" => UnitKind.Witch,
            "wisp" => UnitKind.Wisp,
            "golem" => UnitKind.Golem,
            "catapult" => UnitKind.Catapult,
            "wyvern" => UnitKind.Wyvern,
            "skeleton" => UnitKind.Skeleton,
            _ => throw Unresolved(contentId, "unit"),
        };
    }

    public static bool IsRuinedBuildingState(string? state) =>
        string.Equals(state, "ruined", StringComparison.OrdinalIgnoreCase);

    private static bool IsVanilla(ContentId contentId) =>
        string.Equals(contentId.Namespace, "vanilla", StringComparison.OrdinalIgnoreCase);

    private static MapLoadException Unresolved(ContentId contentId, string contentKind) =>
        new($"Unresolved {contentKind} type '{contentId.Full}'.");
}
