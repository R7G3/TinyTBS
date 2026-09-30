namespace TinyTBS.Rules.Units.Models;

/// <summary>Predicate for a special damage multiplier (GDD COMBAT.md).</summary>
public sealed class UnitSpecialWhenDefinition
{
    public bool IsDefault { get; init; }

    public string? TargetHasTag { get; init; }

    public int? ManhattanRange { get; init; }
}
