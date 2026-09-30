namespace TinyTBS.Scripting.Api;

/// <summary>Readonly unit snapshot for scripts.</summary>
public sealed class MapScriptUnitView
{
    public required int Id { get; init; }
    public required string Type { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int OwnerPlayerIndex { get; init; }
    public required int Hp { get; init; }
    public required int MaxHp { get; init; }
}
