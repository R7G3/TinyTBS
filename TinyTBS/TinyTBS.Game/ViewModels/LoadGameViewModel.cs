namespace TinyTBS.Game.ViewModels;

/// <summary>UI-state for the Load Game screen.</summary>
public sealed class LoadGameViewModel
{
    public string Title { get; set; } = "Load Game";

    public string StatusText { get; set; } = "Confirm a save for Load / Delete. Back returns to the menu.";

    public IReadOnlyList<LoadGameSaveRowViewModel> Saves { get; set; } = [];
}
