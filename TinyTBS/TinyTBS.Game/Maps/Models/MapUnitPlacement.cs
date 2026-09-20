namespace TinyTBS.Game.Maps.Models;

public sealed class MapUnitPlacement
{
    public required ContentId Type { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Slot { get; init; }
    public int? Hp { get; init; }
    public int? Xp { get; init; }
}
