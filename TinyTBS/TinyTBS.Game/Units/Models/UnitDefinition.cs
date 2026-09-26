using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Units.Models;

/// <summary>Domain unit type loaded from a units-module JSON file.</summary>
public sealed class UnitDefinition
{
    public required ContentId ContentId { get; init; }

    public required string DisplayNameKey { get; init; }

    public required string MovementClass { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public bool Recruitable { get; init; }

    public int Attack { get; init; }

    public int Defence { get; init; }

    public int MaxHealth { get; init; }

    public int AttackRangeMin { get; init; }

    public int AttackRangeMax { get; init; }

    public int Speed { get; init; }

    public int Cost { get; init; }

    public IReadOnlyList<UnitAbilityDefinition> Abilities { get; init; } = [];

    public bool LeavesGravestone { get; init; } = true;

    public UnitSpritesDefinition? Sprites { get; init; }

    /// <summary>Absolute root of the units module that owns this definition (for Resources paths).</summary>
    public required string SourceModuleRootPath { get; init; }
}
