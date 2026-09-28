namespace TinyTBS.Game.Scripting.Models;

/// <summary>Read/write API for campaign scripts (meta progress, not map cells).</summary>
public sealed class CampaignScriptContext
{
    private readonly CampaignScriptMutation _mutation;
    private readonly Dictionary<string, string> _extensions;

    internal CampaignScriptContext(
        string campaignId,
        string levelId,
        IReadOnlyDictionary<string, string> extensions,
        CampaignScriptMutation mutation)
    {
        CampaignId = campaignId;
        LevelId = levelId;
        _extensions = extensions is Dictionary<string, string> dictionary
            ? dictionary
            : new Dictionary<string, string>(extensions, StringComparer.Ordinal);
        _mutation = mutation;
    }

    public string CampaignId { get; }

    public string LevelId { get; }

    public string? GetFlag(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var trimmed = key.Trim();
        if (_mutation.Flags.TryGetValue(trimmed, out var pending))
            return pending;
        return _extensions.TryGetValue(trimmed, out var value) ? value : null;
    }

    public void SetFlag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _mutation.Flags[key.Trim()] = value ?? string.Empty;
    }

    /// <summary>After this chapter win, go to <paramref name="levelId"/> instead of linear next.</summary>
    public void SetNextChapter(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return;
        _mutation.ForcedNextLevelId = levelId.Trim();
    }

    /// <summary>Treat <paramref name="fromLevelId"/> as <paramref name="toLevelId"/> in progress unlock.</summary>
    public void ReplaceLevel(string fromLevelId, string toLevelId)
    {
        if (string.IsNullOrWhiteSpace(fromLevelId) || string.IsNullOrWhiteSpace(toLevelId))
            return;
        _mutation.ReplacedLevelId = fromLevelId.Trim();
        _mutation.ReplaceWithLevelId = toLevelId.Trim();
    }

    /// <summary>Skip <paramref name="levelId"/> (progress jumps to the linear next after it).</summary>
    public void SkipChapter(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return;
        _mutation.SkippedLevelId = levelId.Trim();
    }
}
