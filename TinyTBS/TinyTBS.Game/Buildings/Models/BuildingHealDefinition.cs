namespace TinyTBS.Game.Buildings.Models;

/// <summary>Heal rule for a building (intact or ruined override).</summary>
public sealed class BuildingHealDefinition
{
    public int Amount { get; init; }

    public required string Scope { get; init; }
}
