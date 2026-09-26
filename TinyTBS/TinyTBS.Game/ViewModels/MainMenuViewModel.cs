namespace TinyTBS.Game.ViewModels;

/// <summary>UI-state bag for the main menu (not an MVVM architecture layer).</summary>
public sealed class MainMenuViewModel
{
    public string Title { get; set; } = "TinyTBS";

    /// <summary>True when a save exists for Continue (saves not implemented yet).</summary>
    public bool CanContinue { get; set; }

    public bool CanLoadGame { get; set; }

    public bool CanOpenContent { get; set; }

    public bool CanOpenEditor { get; set; }

    public bool CanOpenSettings { get; set; }

    public bool CanOpenAbout { get; set; }

    /// <summary>Temporary: New Game starts vanilla proving-grounds until new-game-flow exists.</summary>
    public bool CanStartNewGame { get; set; } = true;
}
