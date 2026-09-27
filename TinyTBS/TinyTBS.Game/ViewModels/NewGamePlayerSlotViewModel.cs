namespace TinyTBS.Game.ViewModels;

/// <summary>One player slot in the skirmish lobby.</summary>
public sealed class NewGamePlayerSlotViewModel
{
    public required int SlotIndex { get; init; }

    public required NewGamePlayerKind Kind { get; init; }

    public string SummaryLine
    {
        get
        {
            var kindLabel = Kind switch
            {
                NewGamePlayerKind.Bot => "Bot",
                _ => "Local",
            };
            return $"P{SlotIndex + 1}  ·  {kindLabel}";
        }
    }
}
