namespace TinyTBS.Game.Scripting.Models;

/// <summary>Readonly map snapshot for scripts.</summary>
public sealed class MapScriptMapView
{
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>Terrain type ids (e.g. grass, water), indexed [x, y].</summary>
    public required string[,] Surface { get; init; }

    public string GetTerrain(int x, int y) => Surface[x, y];
}
