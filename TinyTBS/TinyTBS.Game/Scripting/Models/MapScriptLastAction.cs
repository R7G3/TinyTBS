namespace TinyTBS.Game.Scripting.Models;

public sealed class MapScriptLastAction
{
    public required MapScriptActionKind Kind { get; init; }
    public required int PlayerIndex { get; init; }
    public int? UnitId { get; init; }
    public int? SourceX { get; init; }
    public int? SourceY { get; init; }
    public int? TargetX { get; init; }
    public int? TargetY { get; init; }
    public string Result { get; init; } = "ok";
}
