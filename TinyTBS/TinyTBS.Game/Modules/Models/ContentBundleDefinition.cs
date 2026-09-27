namespace TinyTBS.Game.Modules.Models;

/// <summary>Loaded <c>*.bundle.json</c> preset (module id list + match defaults).</summary>
public sealed class ContentBundleDefinition
{
    public required string BundleId { get; init; }

    public required string Title { get; init; }

    /// <summary>Module ids advertised by this preset (informational + defaults must reference these).</summary>
    public required IReadOnlyList<string> ModuleIds { get; init; }

    public required ContentBundleDefaults Defaults { get; init; }

    public required string BundleFilePath { get; init; }

    public required ContentModuleSource Source { get; init; }
}
