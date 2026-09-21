namespace TinyTBS.Game.Scripting.Models;

public sealed class MapScriptUnitView
{
    public required int Id { get; init; }
    public required string Type { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int OwnerPlayerIndex { get; init; }
    public int Hp { get; init; } = 100;
}
