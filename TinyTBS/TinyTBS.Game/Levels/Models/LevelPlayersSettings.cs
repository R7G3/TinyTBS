namespace TinyTBS.Game.Levels.Models;

public sealed class LevelPlayersSettings
{
    public required int Min { get; init; }
    public required int Max { get; init; }
    public required int DefaultSlots { get; init; }
}
