using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Units.Models;

/// <summary>Loaded units module: manifest + unit definitions.</summary>
public sealed class UnitModuleDefinition
{
    public required string ModuleId { get; init; }

    public required string ContentNamespace { get; init; }

    public required string Title { get; init; }

    public required string Version { get; init; }

    /// <summary>Logical ids listed in <c>recruit.addsToPool</c> (shop order).</summary>
    public required IReadOnlyList<ContentId> RecruitPool { get; init; }

    public required IReadOnlyDictionary<ContentId, UnitDefinition> UnitsById { get; init; }

    public required string ModuleRootPath { get; init; }
}
