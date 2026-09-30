using TinyTBS.Rules.Match;

namespace TinyTBS.Rules.Ai;

/// <summary>
/// Move selection policy. Today: atomic α-β; Hard may plug in another implementation
/// (for example whole-turn planning) without touching the live bot driver.
/// ---
/// Политика выбора хода. На данный момент: атомарный α-β-поиск;
/// при необходимости можно подключить другую реализацию (например, планирование на весь ход),
/// не затрагивая при этом основной код управления ботом.
/// </summary>
public interface IBotSearchPolicy
{
    /// <param name="state">
    /// Current match state; the search works on clones and never mutates it.
    /// Текущее состояние матча; поиск выполняется на клонах и никогда не изменяет исходный объект.
    /// </param>
    /// <param name="selectedUnitId">
    /// Activation the player is in, if any. Not stored on <paramref name="state"/>.
    /// Состояние активации игрока (при наличии). Не сохраняется в <paramref name="state"/>.
    /// </param>
    /// <param name="botPlayerIndex">
    /// The bot's player index; evaluation is always from its side.
    /// Индекс игрока, соответствующий боту; оценка всегда производится с его стороны.
    /// </param>
    /// <param name="profile">
    /// Depth / node limits and evaluation weights for the difficulty.
    /// Ограничения по глубине / количеству узлов и весовые коэффициенты оценки для определения сложности.
    /// </param>
    BotAtomicAction ChooseAction(
        MatchState state,
        int? selectedUnitId,
        int botPlayerIndex,
        BotDifficultyProfile profile);
}
