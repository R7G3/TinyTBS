using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Units.Models;

internal sealed class UnitAbilityDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("amount")]
    public int? Amount { get; set; }

    [JsonPropertyName("minRange")]
    public int? MinRange { get; set; }

    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("radius")]
    public int? Radius { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}
