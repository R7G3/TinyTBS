namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Focus anchors and steppers produced while filling the Lobby tab list.</summary>
internal sealed class NewGameLobbyTabBodyResult
{
    public int AddPlayerFocusIndex { get; init; } = -1;

    public int AddPlayerTypeLocalFocusIndex { get; init; } = -1;

    public int CancelAddPlayerTypeFocusIndex { get; init; } = -1;

    /// <summary>First enabled per-slot X remove button, if any.</summary>
    public int FirstRemovePlayerFocusIndex { get; init; } = -1;

    public NewGameValueStepperWidgets? GoldStepper { get; init; }

    public NewGameValueStepperWidgets? UnitCapStepper { get; init; }
}

