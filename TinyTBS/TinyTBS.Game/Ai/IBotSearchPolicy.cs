using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ai;

/// <summary>
/// Политика выбора хода. Сейчас — atomic α-β; Hard позже может дать другую реализацию
/// (например, планирование целого хода), не меняя BotTurnDriver.
/// </summary>
public interface IBotSearchPolicy
{
    /// <param name="state">Текущее состояние матча (поиск клонирует сам).</param>
    /// <param name="botPlayerIndex">Индекс игрока-бота — оценка всегда с его стороны.</param>
    /// <param name="profile">Лимиты глубины / узлов и веса оценки для сложности.</param>
    BotAtomicAction ChooseAction(MatchState state, int botPlayerIndex, BotDifficultyProfile profile);
}
