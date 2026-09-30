using TinyTBS.Rules.Match;

namespace TinyTBS.Rules.Maps;

/// <summary>Map terrain type strings and building instance <c>state</c> flags.</summary>
public static class MapSurfaceIds
{
    public const string Grass = "grass";

    public const string Water = "water";

    public const string Road = "road";

    public const string Mountain = "mountain";

    public const string Bridge = "bridge";

    public const string Forest = "forest";

    /// <summary>Building instance <c>state</c> while the building is standing.</summary>
    public const string IntactBuildingState = "intact";

    /// <summary>Building instance <c>state</c> for a destroyed building.</summary>
    public const string RuinedBuildingState = "ruined";

    public static string ToId(TerrainKind kind) => kind switch
    {
        TerrainKind.Water => Water,
        TerrainKind.Road => Road,
        TerrainKind.Mountain => Mountain,
        TerrainKind.Bridge => Bridge,
        TerrainKind.Forest => Forest,
        _ => Grass,
    };

    public static TerrainKind ParseTerrain(string typeId)
    {
        return typeId.Trim().ToLowerInvariant() switch
        {
            Grass => TerrainKind.Grass,
            Water => TerrainKind.Water,
            Road => TerrainKind.Road,
            Mountain => TerrainKind.Mountain,
            Bridge => TerrainKind.Bridge,
            Forest => TerrainKind.Forest,
            _ => throw new MapLoadException($"Unknown terrain type '{typeId}'."),
        };
    }

    public static bool IsRuinedBuildingState(string? state) =>
        string.Equals(state, RuinedBuildingState, StringComparison.OrdinalIgnoreCase);
}
