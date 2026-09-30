using TinyTBS.Rules.Match;

namespace TinyTBS.Rules.Ai;

/// <summary>
/// One bot decision = one node of the α-β tree: not a whole player turn but a single input
/// (select a unit, confirm on a cell, wait, recruit, end turn) together with the rules action it resolves to.
/// </summary>
public sealed class BotAtomicAction
{
    public required BotAtomicActionKind Kind { get; init; }

    /// <summary>What the input does under the current rules; applied with <see cref="MatchState.TryApply"/>.</summary>
    public required MatchAction Action { get; init; }

    /// <summary>
    /// Order among equally scored actions: lower is preferred.
    /// <see cref="LegalActionGenerator"/> assigns stable values (moves before Wait, and so on).
    /// </summary>
    public int TieBreak { get; init; }

    /// <summary>Cell the bot's visible cursor travels to before acting; null for End turn.</summary>
    public GridCell? AimCell => Kind == BotAtomicActionKind.EndTurn ? null : Action.Target;

    /// <summary>
    /// "Noisy" actions change HP, army or gold noticeably; after one, quiescence search looks a couple
    /// of plies deeper instead of evaluating mid-fight.
    /// </summary>
    public bool IsNoisyForQuiescence => Kind is BotAtomicActionKind.ConfirmAt or BotAtomicActionKind.Recruit;
}
