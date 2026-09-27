namespace TinyTBS.Game.ViewModels;

public sealed class ContentModuleRowViewModel
{
    public required string ModuleId { get; init; }

    public required string Title { get; init; }

    public required string TypeLabel { get; init; }

    public required string Version { get; init; }

    public string? Description { get; init; }

    public required string SourceLabel { get; init; }

    public required bool CanUninstall { get; init; }

    /// <summary>Compact list line (version shown in the detail popup).</summary>
    public string SummaryLine => $"{TypeLabel}  ·  {Title}  ·  {SourceLabel}";
}
