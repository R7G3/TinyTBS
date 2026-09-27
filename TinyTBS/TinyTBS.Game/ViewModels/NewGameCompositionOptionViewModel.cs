namespace TinyTBS.Game.ViewModels;

/// <summary>One composition source: scenario defaults or a <c>*.bundle.json</c> preset.</summary>
public sealed class NewGameCompositionOptionViewModel
{
    /// <summary><see cref="NewGameCompositionSources.ScenarioDefaults"/> or a bundle id.</summary>
    public required string SourceId { get; init; }

    public required string Title { get; init; }

    public bool IsSelected { get; init; }

    public string SummaryLine =>
        IsSelected ? $"[ {Title} ]" : Title;
}
