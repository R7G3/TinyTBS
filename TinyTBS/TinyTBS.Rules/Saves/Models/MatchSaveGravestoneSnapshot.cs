using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Saves.Models;

/// <summary>Gravestone row in a match runtime snapshot.</summary>
public sealed class MatchSaveGravestoneSnapshot
{
    [JsonPropertyName("cell")]
    public MatchSaveCell Cell { get; init; } = null!;

    [JsonPropertyName("sourcePlayerIndex")]
    public int SourcePlayerIndex { get; init; }

    [JsonPropertyName("expiresWhenTurnStartsReaches")]
    public int ExpiresWhenTurnStartsReaches { get; init; }
}
