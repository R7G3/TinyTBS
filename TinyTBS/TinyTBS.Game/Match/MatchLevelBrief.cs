namespace TinyTBS.Game.Match;

/// <summary>Level metadata kept on the session for HUD (goals, title).</summary>
public sealed class MatchLevelBrief
{
    public required string LevelId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string VictoryType { get; init; }
    public required string DefeatType { get; init; }
    public required string TeamDefeatMode { get; init; }

    /// <summary>Unit cap chosen at New Game (or level default). Enforcement arrives later.</summary>
    public int UnitCap { get; init; }
}
