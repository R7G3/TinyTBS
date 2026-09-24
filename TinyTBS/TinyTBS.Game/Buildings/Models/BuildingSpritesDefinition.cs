namespace TinyTBS.Game.Buildings.Models;

/// <summary>Sprite paths for a building type (relative to the buildings module root).</summary>
public sealed class BuildingSpritesDefinition
{
    public required string BasePath { get; init; }

    public required string MaskPath { get; init; }

    public string? RuinedBasePath { get; init; }

    public string? RuinedMaskPath { get; init; }
}
