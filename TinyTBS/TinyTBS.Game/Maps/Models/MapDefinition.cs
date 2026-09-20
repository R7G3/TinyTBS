namespace TinyTBS.Game.Maps.Models;

/// <summary>Loaded map folder (map.json + optional script path).</summary>
public sealed class MapDefinition
{
    public required int FormatVersion { get; init; }
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string[,] Surface { get; init; }
    public required IReadOnlyList<MapBuildingPlacement> Buildings { get; init; }
    public required IReadOnlyList<MapUnitPlacement> Units { get; init; }
    public required IReadOnlyList<MapMemorialPlacement> Memorials { get; init; }

    /// <summary>Absolute path to script.cs when present.</summary>
    public string? ScriptPath { get; init; }

    /// <summary>Folder that contained map.json.</summary>
    public string? SourceDirectory { get; init; }
}
