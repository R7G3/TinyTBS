using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Saves.Models;

/// <summary>Runtime match fields restored after map/catalog load.</summary>
public sealed class MatchRuntimeSnapshot
{
    [JsonPropertyName("playerCount")]
    public int PlayerCount { get; init; }

    [JsonPropertyName("currentPlayer")]
    public int CurrentPlayer { get; init; }

    [JsonPropertyName("turnNumber")]
    public int TurnNumber { get; init; }

    [JsonPropertyName("nextUnitId")]
    public int NextUnitId { get; init; }

    [JsonPropertyName("unitCap")]
    public int UnitCap { get; init; }

    [JsonPropertyName("moneyByPlayer")]
    public List<int> MoneyByPlayer { get; init; } = [];

    [JsonPropertyName("turnStartsByPlayer")]
    public List<int> TurnStartsByPlayer { get; init; } = [];

    [JsonPropertyName("kingRehireCountByPlayer")]
    public List<int> KingRehireCountByPlayer { get; init; } = [];

    [JsonPropertyName("eliminatedPlayers")]
    public List<int> EliminatedPlayers { get; init; } = [];

    [JsonPropertyName("cursor")]
    public MatchSaveCell Cursor { get; init; } = null!;

    [JsonPropertyName("selectedUnitId")]
    public int? SelectedUnitId { get; init; }

    [JsonPropertyName("winnerPlayerIndex")]
    public int? WinnerPlayerIndex { get; init; }

    [JsonPropertyName("victoryReason")]
    public string? VictoryReason { get; init; }

    [JsonPropertyName("units")]
    public List<MatchSaveUnitSnapshot> Units { get; init; } = [];

    [JsonPropertyName("buildings")]
    public List<MatchSaveBuildingSnapshot> Buildings { get; init; } = [];

    [JsonPropertyName("gravestones")]
    public List<MatchSaveGravestoneSnapshot> Gravestones { get; init; } = [];
}
