using System.Text.Json.Serialization;

namespace TinyTBS.Game.Saves.Models;

/// <summary>One content id replace entry in a save.</summary>
public sealed class MatchSaveReplace
{
    [JsonPropertyName("from")]
    public string From { get; init; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; init; } = string.Empty;
}
