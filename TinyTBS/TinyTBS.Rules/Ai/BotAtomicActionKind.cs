namespace TinyTBS.Rules.Ai;

/// <summary>The input a human would use for a bot decision (see <see cref="LegalActionGenerator"/>).</summary>
public enum BotAtomicActionKind
{
    /// <summary>End the player's turn.</summary>
    EndTurn,

    /// <summary>Select an own active unit (Confirm on its cell with nothing selected).</summary>
    SelectUnit,

    /// <summary>
    /// Confirm on a cell with a unit selected: move / attack / capture / raise / wait,
    /// resolved by <see cref="Match.MatchActionResolver"/>.
    /// </summary>
    ConfirmAt,

    /// <summary>Finish the unit's activation without attacking (Wait / Y).</summary>
    WaitSelected,

    /// <summary>Buy a unit in an own castle from the shop.</summary>
    Recruit,
}
