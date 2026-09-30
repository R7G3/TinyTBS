using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Buildings.Models;

internal sealed class BuildingHealDto
{
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
