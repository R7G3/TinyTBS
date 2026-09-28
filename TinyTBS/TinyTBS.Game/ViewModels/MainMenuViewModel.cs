namespace TinyTBS.Game.ViewModels;

/// <summary>UI-state bag for the main menu (not an MVVM architecture layer).</summary>
public sealed class MainMenuViewModel
{
    public string Title { get; set; } = "TinyTBS";

    /// <summary>True when a suspended match or disk save is available for Continue.</summary>
    public bool CanContinue { get; set; }

    public bool CanLoadGame { get; set; }

    public bool CanOpenContent { get; set; }

    public bool CanOpenEditor { get; set; }

    public bool CanOpenSettings { get; set; }

    public bool CanOpenAbout { get; set; }

    /// <summary>Opens the New Game flow (scenario + level).</summary>
    public bool CanStartNewGame { get; set; } = true;

    /// <summary>Short status under the menu (abandon confirm, errors).</summary>
    public string StatusHint { get; set; } = string.Empty;
}
