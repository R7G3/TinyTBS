using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Maps;

/// <summary>
/// Resolves vanilla/* logical ids to demo match enums until full unit/building modules exist.
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
            _ => throw Unresolved(contentId, "unit"),
        };
    }

    private static bool IsVanilla(ContentId contentId) =>
        string.Equals(contentId.Namespace, "vanilla", StringComparison.OrdinalIgnoreCase);

    private static MapLoadException Unresolved(ContentId contentId, string contentKind) =>
        new($"Unresolved {contentKind} type '{contentId.Full}'. Only vanilla/* ids are supported until content-mods.");
}
