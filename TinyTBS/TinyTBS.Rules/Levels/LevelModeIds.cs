namespace TinyTBS.Rules.Levels;

/// <summary>Level <c>modes</c> values the game understands (LEVEL_FORMAT).</summary>
public static class LevelModeIds
{
    public const string Skirmish = "skirmish";

    public const string Campaign = "campaign";

    public static bool IsSkirmish(string? mode) =>
        string.Equals(mode, Skirmish, StringComparison.OrdinalIgnoreCase);

    public static bool IsCampaign(string? mode) =>
        string.Equals(mode, Campaign, StringComparison.OrdinalIgnoreCase);
}
