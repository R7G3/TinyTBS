using TinyTBS.Rules.Match;

namespace TinyTBS.Rules.Ai;

/// <summary>
/// One bot decision = one node of the α-β tree: not a whole player turn but a single input
/// (select a unit, confirm on a cell, wait, recruit, end turn) together with the rules action it resolves to.
/// ---
/// Одно атомарное решение бота = один узел в дереве α-β.
/// Не «весь ход игрока», а одна операция: выбрать юнита, Confirm на клетку, Wait, Recruit, EndTurn.
/// </summary>
public sealed class BotAtomicAction
{
    public required BotAtomicActionKind Kind { get; init; }

    /// <summary>
    /// What the input does under the current rules; applied with <see cref="MatchState.TryApply"/>.
    /// ---
    /// Что происходит с входными данными в соответствии с действующими правилами; применяется с <see cref="MatchState.TryApply"/>.
    /// </summary>
    public required MatchAction Action { get; init; }

    /// <summary>
    /// Order among equally scored actions: lower is preferred.
    /// <see cref="LegalActionGenerator"/> assigns stable values (moves before Wait, and so on).
    /// ---
    /// Порядок при равной оценке: меньше = предпочтительнее.
    /// LegalActionGenerator выдаёт стабильные значения (ходы раньше Wait и т.д.).
    /// </summary>
    public int TieBreak { get; init; }

    /// <summary>
    /// Cell the bot's visible cursor travels to before acting; null for End turn.
    /// ---
    /// Клетка, в которую перемещается видимый курсор бота перед выполнением действия; null — для завершения хода.
    /// </summary>
    public GridCell? AimCell => Kind == BotAtomicActionKind.EndTurn ? null : Action.Target;

    /// <summary>
    /// "Noisy" actions change HP, army or gold noticeably; after one, quiescence search looks a couple
    /// of plies deeper instead of evaluating mid-fight.
    /// ---
    /// «Шумные» действия заметно изменяют количество HP, численность армии или запас золота;
    /// после такого действия поиск в состоянии покоя (quiescence search) просматривает дерево игры
    /// на несколько полуходов глубже, вместо того чтобы оценивать позицию в разгар боя.
    /// </summary>
    public bool IsNoisyForQuiescence => Kind is BotAtomicActionKind.ConfirmAt or BotAtomicActionKind.Recruit;
}
