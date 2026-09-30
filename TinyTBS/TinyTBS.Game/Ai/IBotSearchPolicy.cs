using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ai;

/// <summary>
/// Move selection policy. Today: atomic α-β; Hard may plug in another implementation
/// (for example whole-turn planning) without touching the live bot driver.
/// </summary>
public interface IBotSearchPolicy
{
    /// <param name="state">Current match state; the search works on clones and never mutates it.</param>
    /// <param name="botPlayerIndex">The bot's player index; evaluation is always from its side.</param>
    /// <param name="profile">Depth / node limits and evaluation weights for the difficulty.</param>
    BotAtomicAction ChooseAction(MatchState state, int botPlayerIndex, BotDifficultyProfile profile);
}
