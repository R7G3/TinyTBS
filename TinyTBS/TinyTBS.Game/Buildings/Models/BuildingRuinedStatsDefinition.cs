namespace TinyTBS.Game.Buildings.Models;

/// <summary>Stat overrides applied while the building instance is ruined.</summary>
public sealed class BuildingRuinedStatsDefinition
{
    public int Income { get; init; }

    public int DefenceBonus { get; init; }

    public BuildingHealDefinition? Heal { get; init; }

    public bool Capturable { get; init; }
}
