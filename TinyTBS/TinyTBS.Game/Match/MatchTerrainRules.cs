using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>Terrain step costs and defence bonuses (WORLD.md).</summary>
public static class MatchTerrainRules
{
    public static int StepCost(TerrainKind terrain, MovementClass movementClass) =>
        (terrain, movementClass) switch
        {
            (TerrainKind.Road, _) => 1,
            (TerrainKind.Bridge, _) => 1,
            (TerrainKind.Grass, _) => 1,
            (TerrainKind.Forest, MovementClass.Fly) => 1,
            (TerrainKind.Forest, _) => 2,
            (TerrainKind.Mountain, MovementClass.Fly) => 1,
            (TerrainKind.Mountain, MovementClass.Water) => 4,
            (TerrainKind.Mountain, _) => 3,
            (TerrainKind.Water, MovementClass.Water) => 1,
            (TerrainKind.Water, MovementClass.Fly) => 1,
            (TerrainKind.Water, _) => 4,
            _ => 1,
        };

    public static int DefenceBonus(TerrainKind terrain) =>
        terrain switch
        {
            TerrainKind.Grass => 5,
            TerrainKind.Forest => 10,
            TerrainKind.Mountain => 15,
            _ => 0,
        };
}
