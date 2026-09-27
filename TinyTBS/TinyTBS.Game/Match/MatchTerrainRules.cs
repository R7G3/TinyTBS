namespace TinyTBS.Game.Match;

/// <summary>Terrain step costs and defence bonuses (WORLD.md).</summary>
public static class MatchTerrainRules
{
    public static int StepCost(TerrainKind terrain, string movementClass)
    {
        var movement = NormalizeMovementClass(movementClass);
        return (terrain, movement) switch
        {
            (TerrainKind.Road, _) => 1,
            (TerrainKind.Bridge, _) => 1,
            (TerrainKind.Grass, _) => 1,
            (TerrainKind.Forest, "fly") => 1,
            (TerrainKind.Forest, _) => 2,
            (TerrainKind.Mountain, "fly") => 1,
            (TerrainKind.Mountain, "water") => 4,
            (TerrainKind.Mountain, _) => 3,
            (TerrainKind.Water, "water") => 1,
            (TerrainKind.Water, "fly") => 1,
            (TerrainKind.Water, _) => 4,
            _ => 1,
        };
    }

    public static int DefenceBonus(TerrainKind terrain) =>
        terrain switch
        {
            TerrainKind.Grass => 5,
            TerrainKind.Forest => 10,
            TerrainKind.Mountain => 15,
            _ => 0,
        };

    private static string NormalizeMovementClass(string movementClass)
    {
        if (string.IsNullOrWhiteSpace(movementClass))
            return "foot";

        var value = movementClass.Trim().ToLowerInvariant();
        return value switch
        {
            "water" or "aquatic" => "water",
            "fly" or "flying" => "fly",
            _ => "foot",
        };
    }
}
