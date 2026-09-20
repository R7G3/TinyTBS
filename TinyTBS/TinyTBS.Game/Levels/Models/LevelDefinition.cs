using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Levels.Models;

/// <summary>Loaded level folder with resolved map (map.ref only — no embed).</summary>
public sealed class LevelDefinition
{
    public required int FormatVersion { get; init; }
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<string> Modes { get; init; }

    /// <summary>Original <c>map.ref</c> from level.json (e.g. <c>Maps/demo</c>).</summary>
    public required string MapRef { get; init; }

    /// <summary>Map loaded from <see cref="MapRef"/> under the scenario module root.</summary>
    public required MapDefinition Map { get; init; }

    public required LevelPlayersSettings Players { get; init; }
    public required int DefaultStartingGold { get; init; }
    public required int DefaultUnitCap { get; init; }
    public required string TeamDefeatMode { get; init; }
    public required LevelConditionSettings Victory { get; init; }
    public required LevelConditionSettings Defeat { get; init; }
    public LevelDialogsSettings? Dialogs { get; init; }

    /// <summary>Folder that contained level.json.</summary>
    public string? SourceDirectory { get; init; }

    /// <summary>Scenario module root used to resolve <see cref="MapRef"/>.</summary>
    public string? ScenarioModuleRoot { get; init; }
}
