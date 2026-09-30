using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Units.Models;

internal sealed class UnitSpecialWhenDto
{
    [JsonPropertyName("default")]
    public bool? Default { get; set; }

    [JsonPropertyName("targetHasTag")]
    public string? TargetHasTag { get; set; }

    [JsonPropertyName("manhattanRange")]
    public int? ManhattanRange { get; set; }
}
