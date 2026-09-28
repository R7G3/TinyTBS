namespace TinyTBS.Game.ViewModels;

/// <summary>One player slot in the New Game lobby.</summary>
public sealed class NewGamePlayerSlotViewModel
{
    public required int SlotIndex { get; init; }

    public required NewGamePlayerKind Kind { get; init; }

    /// <summary>Used when <see cref="Kind"/> is Bot.</summary>
    public TinyTBS.Game.Ai.BotDifficulty BotDifficulty { get; init; }

    /// <summary>Index into <c>PlayerPalette</c> (color picker later).</summary>
    public required int PaletteIndex { get; init; }

    public string SummaryLine
    {
        get
        {
            var kindLabel = Kind switch
            {
                NewGamePlayerKind.Bot => BotDifficulty switch
                {
                    TinyTBS.Game.Ai.BotDifficulty.Normal => "Bot · Normal",
                    TinyTBS.Game.Ai.BotDifficulty.Hard => "Bot · Hard",
                    _ => "Bot · Easy",
                },
                NewGamePlayerKind.Remote => "Remote",
                _ => "Local",
            };
            return $"P{SlotIndex + 1}  ·  {kindLabel}";
        }
    }
}

