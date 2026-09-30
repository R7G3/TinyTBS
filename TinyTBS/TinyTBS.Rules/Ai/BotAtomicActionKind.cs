namespace TinyTBS.Rules.Ai;

/// <summary>
/// The input a human would use for a bot decision (see <see cref="LegalActionGenerator"/>).
/// ---
/// Входные данные, которые человек использовал бы для принятия решения ботом (см. <see cref="LegalActionGenerator"/>).
/// </summary>
public enum BotAtomicActionKind
{
    /// <summary>End the player's turn.</summary>
    EndTurn,

    /// <summary>
    /// Select an own active unit (Confirm on its cell with nothing selected).
    /// ---
    /// Выбор собственного активного юнита (подтвердите выбор, нажав на его клетку, когда ничего не выделено)/
    /// </summary>
    SelectUnit,

    /// <summary>
    /// Confirm on a cell with a unit selected: move / attack / capture / raise / wait,
    /// resolved by <see cref="Match.MatchActionResolver"/>.
    /// ---
    /// Подтверждение действия на клетке с выбранным юнитом:
    /// перемещение / атака / захват / поднятие / ожидание;
    /// выполнение обеспечивается <see cref="Match.MatchActionResolver"/>.
    /// </summary>
    ConfirmAt,

    /// <summary>
    /// Finish the unit's activation without attacking (Wait / Y).
    /// ---
    /// Завершение активации юнита, не атакуя (Ожидание / Y).
    /// </summary>
    WaitSelected,

    /// <summary>
    /// Buy a unit in an own castle from the shop.
    /// ---
    /// Покупка юнита в магазине собственного замка.
    /// </summary>
    Recruit,
}
