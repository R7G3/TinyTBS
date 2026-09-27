namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Focus anchors and steppers produced while filling the Lobby tab list.</summary>
internal sealed class NewGameLobbyTabBodyResult
{
    public int LobbySessionFocusIndex { get; init; } = -1;

    public int AddPlayerFocusIndex { get; init; } = -1;

    public int RemovePlayerFocusIndex { get; init; } = -1;

    public NewGameValueStepperWidgets? GoldStepper { get; init; }

    public NewGameValueStepperWidgets? UnitCapStepper { get; init; }
}
