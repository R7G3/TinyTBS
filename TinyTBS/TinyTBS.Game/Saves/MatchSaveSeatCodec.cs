using TinyTBS.Rules.Ai;
using TinyTBS.Rules.Saves.Models;

namespace TinyTBS.Game.Saves;

/// <summary>Encode / decode lobby seats for match saves.</summary>
public static class MatchSaveSeatCodec
{
    public static MatchSaveSeat ToSave(MatchPlayerSeat seat)
    {
        ArgumentNullException.ThrowIfNull(seat);

        if (seat.Kind == MatchPlayerKind.Bot)
        {
            return new MatchSaveSeat
            {
                Kind = "bot",
                BotDifficulty = seat.BotDifficulty switch
                {
                    BotDifficulty.Normal => "normal",
                    BotDifficulty.Hard => "hard",
                    _ => "easy",
                },
            };
        }

        return new MatchSaveSeat { Kind = "local", BotDifficulty = null };
    }

    public static MatchPlayerSeat FromSave(MatchSaveSeat seat)
    {
        ArgumentNullException.ThrowIfNull(seat);

        if (string.Equals(seat.Kind, "bot", StringComparison.OrdinalIgnoreCase))
        {
            var difficulty = seat.BotDifficulty?.ToLowerInvariant() switch
            {
                "normal" => BotDifficulty.Normal,
                "hard" => BotDifficulty.Hard,
                _ => BotDifficulty.Easy,
            };
            return new MatchPlayerSeat { Kind = MatchPlayerKind.Bot, BotDifficulty = difficulty };
        }

        return new MatchPlayerSeat { Kind = MatchPlayerKind.Local };
    }

    public static IReadOnlyList<MatchPlayerSeat> FromSaveList(IReadOnlyList<MatchSaveSeat> seats)
    {
        ArgumentNullException.ThrowIfNull(seats);
        return seats.Select(FromSave).ToList();
    }
}
