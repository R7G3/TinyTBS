namespace TinyTBS.Game.Match.Session;

/// <summary>What the match result overlay offers after a campaign chapter ends.</summary>
public sealed record MatchCampaignResult(bool ShowNextChapter, bool ShowRetry, string? Note);
