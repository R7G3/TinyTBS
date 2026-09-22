using Microsoft.Xna.Framework;

namespace TinyTBS.Game.Match;

/// <summary>Terrain tint colors for the match minimap (not full tile art).</summary>
public static class MatchMinimapColors
{
    public static Color ForTerrain(TerrainKind terrain) => terrain switch
    {
        TerrainKind.Water => new Color(40, 80, 160),
        TerrainKind.Road => new Color(120, 110, 90),
        TerrainKind.Mountain => new Color(90, 90, 90),
        TerrainKind.Bridge => new Color(140, 100, 60),
        TerrainKind.Forest => new Color(30, 90, 40),
        _ => new Color(50, 110, 50),
    };
}
