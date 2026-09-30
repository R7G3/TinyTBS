using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Buildings.Models;

internal sealed class BuildingRuinedDto
{
    [JsonPropertyName("income")]
    public int Income { get; set; }

    [JsonPropertyName("defenceBonus")]
    public int DefenceBonus { get; set; }

    [JsonPropertyName("heal")]
    public BuildingHealDto? Heal { get; set; }

    [JsonPropertyName("capturable")]
    public bool Capturable { get; set; }
}
