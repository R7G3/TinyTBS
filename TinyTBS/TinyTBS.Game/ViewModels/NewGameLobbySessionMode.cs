namespace TinyTBS.Game.ViewModels;

/// <summary>How players join the match from the Lobby tab.</summary>
public enum NewGameLobbySessionMode
{
    Hotseat = 0,

    /// <summary>Reserved; greyed in UI until network work.</summary>
    Network = 1,
}
