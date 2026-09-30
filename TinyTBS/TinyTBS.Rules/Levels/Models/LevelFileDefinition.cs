namespace TinyTBS.Rules.Levels.Models;

/// <summary>level.json after the same validation as a match load, before the map is resolved.</summary>
public sealed class LevelFileDefinition
{
    public required int FormatVersion { get; init; }

    public required string Id { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required IReadOnlyList<string> Modes { get; init; }

    /// <summary>Original <c>map.ref</c> (for example <c>Maps/demo</c>).</summary>
    public required string MapRef { get; init; }

    public required LevelPlayersSettings Players { get; init; }

    public required int DefaultStartingGold { get; init; }

    public required int DefaultUnitCap { get; init; }

    public required string TeamDefeatMode { get; init; }

    public required LevelConditionSettings Victory { get; init; }

    public required LevelConditionSettings Defeat { get; init; }

    public LevelDialogsSettings? Dialogs { get; init; }
}
