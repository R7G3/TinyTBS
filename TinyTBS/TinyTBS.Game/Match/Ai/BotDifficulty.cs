namespace TinyTBS.Game.Match.Ai;

/// <summary>
/// Search strength. Hard is reserved for a later full-turn (or deeper) policy —
/// not used in the Easy/Normal lobby slice.
/// </summary>
public enum BotDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
}
