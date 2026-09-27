namespace TinyTBS.Game.ViewModels;

/// <summary>One player slot in the New Game lobby.</summary>
public sealed class NewGamePlayerSlotViewModel
{
    public required int SlotIndex { get; init; }

    public required NewGamePlayerKind Kind { get; init; }

    /// <summary>Index into <c>PlayerPalette</c> (color picker later).</summary>
    public required int PaletteIndex { get; init; }

    public string SummaryLine
    {
        get
        {
            var kindLabel = Kind switch
            {
                NewGamePlayerKind.Bot => "Bot",
                NewGamePlayerKind.Remote => "Remote",
                _ => "Local",
            };
            return $"P{SlotIndex + 1}  ·  {kindLabel}";
        }
    }
}

