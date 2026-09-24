using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Buildings.Models;

/// <summary>Loaded buildings module: manifest + building definitions.</summary>
public sealed class BuildingModuleDefinition
{
    public required string ModuleId { get; init; }

    public required string ContentNamespace { get; init; }

    public required string Title { get; init; }

    public required string Version { get; init; }

    public required IReadOnlyDictionary<ContentId, BuildingDefinition> BuildingsById { get; init; }

    public required string ModuleRootPath { get; init; }
}
