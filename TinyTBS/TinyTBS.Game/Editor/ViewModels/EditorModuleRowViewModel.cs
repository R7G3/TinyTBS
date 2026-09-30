using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Editor.ViewModels;

/// <summary>One module row in the Editor hub list.</summary>
public sealed class EditorModuleRowViewModel
{
    public required string ModuleId { get; init; }

    public required string Title { get; init; }

    public required ContentModuleType Type { get; init; }

    public required ContentModuleSource Source { get; init; }

    public required string ModuleRootPath { get; init; }

    public string SummaryLine =>
        Source == ContentModuleSource.UserLibrary
            ? $"[user] {ModuleId} — {Title} ({Type})"
            : $"[bundled] {ModuleId} — {Title} ({Type})";
}
