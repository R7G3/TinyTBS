namespace TinyTBS.Rules.Ai;

/// <summary>Слот лобби / матча: Local или Bot + сложность.</summary>
public sealed class MatchPlayerSeat
{
    public required MatchPlayerKind Kind { get; init; }

    /// <summary>Имеет смысл при <see cref="MatchPlayerKind.Bot"/>.</summary>
    public BotDifficulty BotDifficulty { get; set; } = BotDifficulty.Easy;
}
