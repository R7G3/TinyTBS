namespace TinyTBS.Game.Match.Ai;

/// <summary>
/// Search policy seam: Easy/Normal use atomic αβ; Hard may later use a full-turn planner
/// without replacing this interface.
/// </summary>
public interface IBotSearchPolicy
{
    BotAtomicAction ChooseAction(MatchState state, int botPlayerIndex, BotDifficultyProfile profile);
}
