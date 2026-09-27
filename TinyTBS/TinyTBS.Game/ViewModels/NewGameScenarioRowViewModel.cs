namespace TinyTBS.Game.ViewModels;

/// <summary>One scenario module row on the New Game screen.</summary>
public sealed class NewGameScenarioRowViewModel
{
    public required string ModuleId { get; init; }

    public required string Title { get; init; }

    public required string SourceLabel { get; init; }

    public bool IsSelected { get; init; }

    public string SummaryLine =>
        IsSelected
            ? $"[ {Title} · {SourceLabel} ]"
            : $"{Title} · {SourceLabel}";
}
