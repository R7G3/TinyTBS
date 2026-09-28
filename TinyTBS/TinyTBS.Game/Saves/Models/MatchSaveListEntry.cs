namespace TinyTBS.Game.Saves.Models;

/// <summary>Lightweight row for save lists / Continue.</summary>
public sealed class MatchSaveListEntry
{
    public required string FilePath { get; init; }

    public required DateTimeOffset WrittenAtUtc { get; init; }

    public required string LevelId { get; init; }

    public required string ScenarioModuleId { get; init; }
}
