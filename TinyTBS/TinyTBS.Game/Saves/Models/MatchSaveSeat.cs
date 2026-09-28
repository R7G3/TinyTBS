using System.Text.Json.Serialization;

namespace TinyTBS.Game.Saves.Models;

/// <summary>Lobby seat snapshot for match resume.</summary>
public sealed class MatchSaveSeat
{
    /// <summary><c>local</c> or <c>bot</c>.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "local";

    /// <summary><c>easy</c> / <c>normal</c> / <c>hard</c>; ignored for local.</summary>
    [JsonPropertyName("botDifficulty")]
    public string? BotDifficulty { get; init; }
}
