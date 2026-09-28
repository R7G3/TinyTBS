using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ai;

/// <summary>
/// Применяет атомарное действие к MatchState (живой матч или клон для поиска).
/// В поиске вызывается напрямую; в игре — через BotTurnDriver → GameplaySession.
/// </summary>
public static class BotAtomicActionApplicator
{
    public static void Apply(MatchState match, BotAtomicAction action)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(action);

        switch (action.Kind)
        {
            case BotAtomicActionKind.EndTurn:
                match.EndTurn();
                break;
            case BotAtomicActionKind.SelectUnit:
            case BotAtomicActionKind.ConfirmAt:
                // Имитация клика: поставить курсор и Confirm (выбор юнита / ход / удар).
                match.HandlePointer(action.Cell);
                match.HandleConfirm();
                break;
            case BotAtomicActionKind.WaitSelected:
                match.TryWaitSelectedUnit();
                break;
            case BotAtomicActionKind.Recruit:
                if (match.ContentCatalog.TryGetUnit(action.UnitTypeId, out var definition))
                {
                    match.TryRecruitAtCastle(
                        action.UnitTypeId,
                        action.RecruitCost,
                        definition.MaxHealth,
                        action.Cell);
                }

                break;
        }
    }

    /// <summary>
    /// «Шумный» ход для quiescence: меняет HP/состав/золото заметно.
    /// После такого хода Normal смотрит ещё пару ply, а не сразу Evaluate.
    /// </summary>
    public static bool IsNoisyForQuiescence(BotAtomicAction action) =>
        action.Kind is BotAtomicActionKind.ConfirmAt or BotAtomicActionKind.Recruit;
}
