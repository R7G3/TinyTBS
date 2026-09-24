namespace TinyTBS.Game.Units.Models;

/// <summary>Sprite paths for a unit type (relative to the units module root).</summary>
public sealed class UnitSpritesDefinition
{
    public required string BasePath { get; init; }

    public required string MaskPath { get; init; }
}
