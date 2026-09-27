namespace TinyTBS.Game.ViewModels;

public sealed class ContentBundleRowViewModel
{
    public required string BundleId { get; init; }

    public required string Title { get; init; }

    public required string SourceLabel { get; init; }

    public required string ModulesSummary { get; init; }

    public string SummaryLine => $"{Title}  ·  {SourceLabel}";

    public string DetailLine => ModulesSummary;
}
