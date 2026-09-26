namespace TinyTBS.Game.Modules.Models;

/// <summary>Lightweight library entry from <c>module.json</c> (no full type-specific load).</summary>
public sealed class ContentModuleInfo
{
    public required string ModuleId { get; init; }

    public required ContentModuleType Type { get; init; }

    public required string ContentNamespace { get; init; }

    public required string Title { get; init; }

    public required string Version { get; init; }

    public required string ModuleRootPath { get; init; }

    public required ContentModuleSource Source { get; init; }
}
