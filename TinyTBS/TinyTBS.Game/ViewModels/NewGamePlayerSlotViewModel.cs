using TinyTBS.Rules.Ai;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.ViewModels;

/// <summary>One player slot in the New Game lobby.</summary>
public sealed class NewGamePlayerSlotViewModel
{
    public required int SlotIndex { get; init; }

    public required NewGamePlayerKind Kind { get; init; }

    /// <summary>Used when <see cref="Kind"/> is Bot.</summary>
    public BotDifficulty BotDifficulty { get; init; }

    /// <summary>Index into <c>PlayerPalette</c> (color picker later).</summary>
    public required int PaletteIndex { get; init; }

    public string SummaryLine =>
        PlayerDisplayNames.ForLobby(
            SlotIndex,
            isBot: Kind == NewGamePlayerKind.Bot,
            isRemote: Kind == NewGamePlayerKind.Remote,
            BotDifficulty);
}
