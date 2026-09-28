namespace TinyTBS.Game.Match.Ai;

/// <summary>Atomic bot decision kinds (one tree node = one of these).</summary>
public enum BotAtomicActionKind
{
    EndTurn,
    SelectUnit,
    ConfirmAt,
    WaitSelected,
    Recruit,
}
