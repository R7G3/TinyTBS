namespace TinyTBS.Game.Ai;

/// <summary>Виды атомарных действий бота (см. LegalActionGenerator).</summary>
public enum BotAtomicActionKind
{
    /// <summary>Закончить ход игрока.</summary>
    EndTurn,

    /// <summary>Выбрать своего активного юнита (Confirm на его клетку без выбранного).</summary>
    SelectUnit,

    /// <summary>
    /// Confirm на клетке при уже выбранном юните: ход / атака / захват / raise —
    /// точный исход решает MatchState.HandleConfirm.
    /// </summary>
    ConfirmAt,

    /// <summary>Явно завершить активацию юнита без атаки (кнопка Wait / Y).</summary>
    WaitSelected,

    /// <summary>Купить юнита в своём замке (клетка свободна, хватает золота).</summary>
    Recruit,
}
