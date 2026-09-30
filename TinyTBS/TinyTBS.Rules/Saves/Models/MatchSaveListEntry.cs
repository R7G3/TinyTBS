namespace TinyTBS.Rules.Saves.Models;

/// <summary>Lightweight row for save lists / Continue.</summary>
public sealed class MatchSaveListEntry
{
    public required string FilePath { get; init; }

    public required DateTimeOffset WrittenAtUtc { get; init; }

    public required string LevelId { get; init; }

    public required string ScenarioModuleId { get; init; }

    public string DisplayTitle => LevelId;

    public string DisplayMeta =>
        $"{ScenarioModuleId} · {WrittenAtUtc.ToUniversalTime():yyyy-MM-dd HH:mm} UTC";
}
