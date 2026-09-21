namespace TinyTBS.Game.Scripting.Models;

public sealed class MapScriptBuildingView
{
    public required string Type { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public int? OwnerPlayerIndex { get; init; }
    public string? State { get; init; }
}
