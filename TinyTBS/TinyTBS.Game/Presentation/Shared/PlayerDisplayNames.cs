using TinyTBS.Rules.Ai;

namespace TinyTBS.Game.Presentation.Shared;

/// <summary>Human-readable player labels (1-based). Lobby, HUD, editor, info panels.</summary>
public static class PlayerDisplayNames
{
    /// <summary>Zero-based slot → "Player 1".</summary>
    public static string Number(int zeroBasedIndex) =>
        "Player " + (zeroBasedIndex + 1);

    public static string Neutral => "Neutral";

    public static string EditorOwner(int? zeroBasedSlot) =>
        zeroBasedSlot is int slot ? Number(slot) : Neutral;

    /// <summary>Units cannot be Neutral — placement falls back to Player 1.</summary>
    public static string EditorUnitPlace(int? zeroBasedSlot) =>
        zeroBasedSlot is int slot
            ? Number(slot)
            : Number(0) + " (Neutral→" + Number(0) + ")";

    public static string ForSeat(int zeroBasedIndex, MatchPlayerSeat seat)
    {
        ArgumentNullException.ThrowIfNull(seat);
        return ForMatchKind(zeroBasedIndex, seat.Kind, seat.BotDifficulty);
    }

    public static string ForMatchKind(int zeroBasedIndex, MatchPlayerKind kind, BotDifficulty botDifficulty) =>
        kind == MatchPlayerKind.Bot
            ? Number(zeroBasedIndex) + " (bot " + FormatBotDifficulty(botDifficulty) + ")"
            : Number(zeroBasedIndex) + " (local)";

    /// <summary>Lobby slot (includes Remote greyed option).</summary>
    public static string ForLobby(
        int zeroBasedIndex,
        bool isBot,
        bool isRemote,
        BotDifficulty botDifficulty)
    {
        if (isBot)
            return Number(zeroBasedIndex) + " (bot " + FormatBotDifficulty(botDifficulty) + ")";
        if (isRemote)
            return Number(zeroBasedIndex) + " (remote)";
        return Number(zeroBasedIndex) + " (local)";
    }

    public static string FormatBotDifficulty(BotDifficulty difficulty) => difficulty switch
    {
        BotDifficulty.Normal => "Normal",
        BotDifficulty.Hard => "Hard",
        _ => "Easy",
    };
}
