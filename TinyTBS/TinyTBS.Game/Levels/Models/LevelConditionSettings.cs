namespace TinyTBS.Game.Levels.Models;

/// <summary>Victory or defeat rule block from level.json.</summary>
public sealed class LevelConditionSettings
{
    public required string Type { get; init; }
}
