namespace TinyTBS.Game.Scripting.Models;

/// <summary>Mutations collected from a campaign script hook invocation.</summary>
public sealed class CampaignScriptMutation
{
    public Dictionary<string, string> Flags { get; } = new(StringComparer.Ordinal);

    public string? ForcedNextLevelId { get; set; }

    public string? ReplacedLevelId { get; set; }

    public string? ReplaceWithLevelId { get; set; }

    public string? SkippedLevelId { get; set; }
}
