using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Editor.ViewModels;

/// <summary>One bundle row on the Editor hub library.</summary>
public sealed class EditorBundleRowViewModel
{
    public required string BundleId { get; init; }

    public required string Title { get; init; }

    public required ContentModuleSource Source { get; init; }

    public required string BundleFilePath { get; init; }

    public string SummaryLine =>
        Source == ContentModuleSource.UserLibrary
            ? $"Bundle: {BundleId} — {Title} (user)"
            : $"Bundle: {BundleId} — {Title} (bundled · Confirm = copy)";
}
