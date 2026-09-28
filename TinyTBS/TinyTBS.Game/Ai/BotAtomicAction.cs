using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ai;

/// <summary>
/// Одно атомарное решение бота = один узел в дереве α-β.
/// Не «весь ход игрока», а одна операция: выбрать юнита, Confirm на клетку, Wait, Recruit, EndTurn.
/// </summary>
public sealed class BotAtomicAction
{
    public required BotAtomicActionKind Kind { get; init; }

    public int UnitId { get; init; }

    public GridCell Cell { get; init; }

    public ContentId UnitTypeId { get; init; }

    public int RecruitCost { get; init; }

    /// <summary>
    /// Порядок при равной оценке: меньше = предпочтительнее.
    /// LegalActionGenerator выдаёт стабильные значения (ходы раньше Wait и т.д.).
    /// </summary>
    public int TieBreak { get; init; }
}
