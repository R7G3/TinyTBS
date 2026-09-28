using System.Text.Json.Serialization;

namespace TinyTBS.Game.Saves.Models;

/// <summary>Cell coordinates in a match save.</summary>
public sealed class MatchSaveCell
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }
}
