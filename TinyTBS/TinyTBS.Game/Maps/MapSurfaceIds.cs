using TinyTBS.Game.Match;

namespace TinyTBS.Game.Maps;

/// <summary>Parses map terrain type strings and building instance state flags.</summary>
public static class MapSurfaceIds
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

    public static bool IsRuinedBuildingState(string? state) =>
        string.Equals(state, "ruined", StringComparison.OrdinalIgnoreCase);
}
