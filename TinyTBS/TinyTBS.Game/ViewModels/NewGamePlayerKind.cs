namespace TinyTBS.Game.ViewModels;

/// <summary>
/// How a lobby slot is filled. Match is always turn-based;
/// Local = this device, Bot = AI (soon), Remote = network (soon).
/// </summary>
public enum NewGamePlayerKind
{
    Local = 0,
    Bot = 1,
    Remote = 2,
}

