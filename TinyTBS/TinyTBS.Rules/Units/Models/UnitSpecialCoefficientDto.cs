using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Units.Models;

internal sealed class UnitSpecialCoefficientDto
{
    [JsonPropertyName("when")]
    public UnitSpecialWhenDto? When { get; set; }

    [JsonPropertyName("multiply")]
    public double Multiply { get; set; }
}
