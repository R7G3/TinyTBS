namespace TinyTBS.Game.Ai;

/// <summary>
/// Сложность бота в лобби. Меняет только поиск и эвристику (BotDifficultyProfile),
/// не урон и не золото. Hard зарезервирован под будущую политику.
/// </summary>
public enum BotDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
}
