namespace TinyTBS.Game.Units.Models;

/// <summary>One engine ability verb declared on a unit type.</summary>
public sealed class UnitAbilityDefinition
{
    public required string Type { get; init; }

    public int? Amount { get; init; }

    public int? MinRange { get; init; }

    public int? Value { get; init; }

    public int? Radius { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];
}
