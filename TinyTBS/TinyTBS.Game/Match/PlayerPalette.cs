using Microsoft.Xna.Framework;

namespace TinyTBS.Game.Match;

/// <summary>Team / neutral tint colors for base+mask sprites.</summary>
public static class PlayerPalette
{
    public static readonly Color Neutral = new(170, 170, 175);

    private static readonly Color[] Players =
    [
        new(70, 140, 255),
        new(230, 70, 70),
        new(80, 200, 90),
        new(240, 200, 60),
        new(180, 90, 220),
        new(50, 200, 200),
        new(255, 140, 50),
        new(240, 120, 180),
        new(140, 100, 70),
        new(120, 200, 255),
    ];

    public static Color ForPlayer(int playerIndex) =>
        Players[Math.Abs(playerIndex) % Players.Length];

    public static Color ForOwner(int? ownerPlayerIndex) =>
        ownerPlayerIndex is int index ? ForPlayer(index) : Neutral;
}
