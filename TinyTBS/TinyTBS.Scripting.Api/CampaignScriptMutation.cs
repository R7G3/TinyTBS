namespace TinyTBS.Scripting.Api;

/// <summary>Changes requested by one campaign script hook; the host applies them to progress.</summary>
public sealed class CampaignScriptMutation
{
    private readonly Dictionary<string, string> _flags = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Flags => _flags;

    public string? ForcedNextLevelId { get; internal set; }

    public string? ReplacedLevelId { get; internal set; }

    public string? ReplaceWithLevelId { get; internal set; }

    public string? SkippedLevelId { get; internal set; }

    internal void SetFlag(string key, string value) => _flags[key] = value;
}
