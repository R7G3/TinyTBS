using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Saves.Models;

/// <summary>Unit row in a match runtime snapshot.</summary>
public sealed class MatchSaveUnitSnapshot
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("typeId")]
    public string TypeId { get; init; } = string.Empty;

    [JsonPropertyName("cell")]
    public MatchSaveCell Cell { get; init; } = null!;

    [JsonPropertyName("playerIndex")]
    public int PlayerIndex { get; init; }

    [JsonPropertyName("maxHealth")]
    public int MaxHealth { get; init; }

    [JsonPropertyName("hitPoints")]
    public int HitPoints { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("hasMovedThisActivation")]
    public bool HasMovedThisActivation { get; init; }

    [JsonPropertyName("cellBeforeMove")]
    public MatchSaveCell CellBeforeMove { get; init; } = null!;

    [JsonPropertyName("experience")]
    public int Experience { get; init; }
}
