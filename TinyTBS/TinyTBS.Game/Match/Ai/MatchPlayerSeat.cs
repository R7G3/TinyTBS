namespace TinyTBS.Game.Match.Ai;

/// <summary>Lobby/match seat: human or bot with optional difficulty.</summary>
public sealed class MatchPlayerSeat
{
    public required MatchPlayerKind Kind { get; init; }

    /// <summary>Meaningful when <see cref="Kind"/> is <see cref="MatchPlayerKind.Bot"/>.</summary>
    public BotDifficulty BotDifficulty { get; set; } = BotDifficulty.Easy;
}
