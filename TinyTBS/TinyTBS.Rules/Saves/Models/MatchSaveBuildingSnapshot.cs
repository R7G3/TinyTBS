using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Saves.Models;

/// <summary>Building row in a match runtime snapshot.</summary>
public sealed class MatchSaveBuildingSnapshot
{
    [JsonPropertyName("typeId")]
    public string TypeId { get; init; } = string.Empty;

    [JsonPropertyName("cell")]
    public MatchSaveCell Cell { get; init; } = null!;

    [JsonPropertyName("ownerPlayerIndex")]
    public int? OwnerPlayerIndex { get; init; }

    [JsonPropertyName("isRuined")]
    public bool IsRuined { get; init; }

    [JsonPropertyName("allowsRecruit")]
    public bool AllowsRecruit { get; init; }

    [JsonPropertyName("repairedThisOwnerTurn")]
    public bool RepairedThisOwnerTurn { get; init; }
}
