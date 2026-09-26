namespace TinyTBS.Game.Modules.Models;

/// <summary>Loaded scenario module manifest (maps/levels live under <see cref="ModuleRootPath"/>).</summary>
public sealed class ScenarioModuleDefinition
{
    public required string ModuleId { get; init; }

    public required string ContentNamespace { get; init; }

    public required string Title { get; init; }

    public required string Version { get; init; }

    public required string ModuleRootPath { get; init; }

    public required ScenarioContentDefaults Defaults { get; init; }

    public required ScenarioContentRequires Requires { get; init; }

    public required IReadOnlyList<ContentIdReplace> Replaces { get; init; }
}
