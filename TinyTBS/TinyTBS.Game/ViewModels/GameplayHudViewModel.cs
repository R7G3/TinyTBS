namespace TinyTBS.Game.ViewModels;

/// <summary>
/// UI-state bag for gameplay HUD (not an MVVM architecture layer).
/// </summary>
public sealed class GameplayHudViewModel
{
    public string StatusText { get; set; } = string.Empty;

    public string HintText { get; } =
        "Arrows/WASD: cursor · Enter/Space: select or move · E: end turn · Esc: menu";
}
