using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match.Ai;

/// <summary>Applies a <see cref="BotAtomicAction"/> to a <see cref="MatchState"/> (live or AI clone).</summary>
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

    public static bool IsNoisyForQuiescence(BotAtomicAction action) =>
        action.Kind is BotAtomicActionKind.ConfirmAt or BotAtomicActionKind.Recruit;
}
