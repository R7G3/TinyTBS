using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Buildings.Models;

/// <summary>Domain building type loaded from a buildings-module JSON file.</summary>
public sealed class BuildingDefinition
{
    public required ContentId ContentId { get; init; }

    public required string DisplayNameKey { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public required BuildingSpritesDefinition Sprites { get; init; }

    public int Income { get; init; }

    public int DefenceBonus { get; init; }

    public bool AllowsRecruit { get; init; }

    public IReadOnlyList<string> RecruitFromTags { get; init; } = [];

    public BuildingHealDefinition? Heal { get; init; }

    public bool Capturable { get; init; }

    public bool Destroyable { get; init; }

    public bool Repairable { get; init; }

    public BuildingRuinedStatsDefinition? Ruined { get; init; }

    public bool CountsTowardPlayerDefeat { get; init; }
}
