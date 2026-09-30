namespace TinyTBS.Rules.Modules.Models;

/// <summary>One playable level discovered under a scenario module's <c>Levels/</c> folder.</summary>
public sealed class ScenarioLevelInfo
{
    public required string LevelId { get; init; }

    public required string Title { get; init; }

    /// <summary>Absolute folder containing <c>level.json</c>.</summary>
    public required string LevelFolderPath { get; init; }

    public required IReadOnlyList<string> Modes { get; init; }

    public required int PlayersMin { get; init; }

    public required int PlayersMax { get; init; }

    public required int PlayersDefaultSlots { get; init; }

    public required int DefaultStartingGold { get; init; }

    public required int DefaultUnitCap { get; init; }

    public bool SupportsSkirmish =>
        Modes.Any(mode => string.Equals(mode, "skirmish", StringComparison.OrdinalIgnoreCase));

    public bool SupportsCampaign =>
        Modes.Any(mode => string.Equals(mode, "campaign", StringComparison.OrdinalIgnoreCase));
}
