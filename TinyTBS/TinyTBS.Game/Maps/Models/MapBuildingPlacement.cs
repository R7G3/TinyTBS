namespace TinyTBS.Game.Maps.Models;

public sealed class MapBuildingPlacement
{
    public required ContentId Type { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public int? Slot { get; init; }
    public string? State { get; init; }
}
