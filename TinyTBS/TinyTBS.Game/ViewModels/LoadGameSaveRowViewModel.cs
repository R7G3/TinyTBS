namespace TinyTBS.Game.ViewModels;

/// <summary>One match-save row on the Load Game list.</summary>
public sealed class LoadGameSaveRowViewModel
{
    public required string FilePath { get; init; }

    public required string Title { get; init; }

    public required string Meta { get; init; }
}
