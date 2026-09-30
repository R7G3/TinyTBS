namespace TinyTBS.Scripting.Api;

/// <summary>Readonly building snapshot for scripts.</summary>
public sealed class MapScriptBuildingView
{
    public required string Type { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public int? OwnerPlayerIndex { get; init; }

    /// <summary><c>ruined</c> for destroyed buildings; null when intact (same values as map <c>state</c>).</summary>
    public string? State { get; init; }
}
