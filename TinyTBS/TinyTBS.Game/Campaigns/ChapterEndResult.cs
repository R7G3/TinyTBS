namespace TinyTBS.Game.Campaigns;

/// <summary>Outcome of resolving a finished campaign chapter.</summary>
public sealed class ChapterEndResult
{
    public required bool PlayerWon { get; init; }

    public required bool HasNextChapter { get; init; }

    public string? NextLevelId { get; init; }
}
