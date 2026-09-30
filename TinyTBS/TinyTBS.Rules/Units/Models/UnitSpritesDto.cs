using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Units.Models;

internal sealed class UnitSpritesDto
{
    [JsonPropertyName("base")]
    public string? Base { get; set; }

    [JsonPropertyName("mask")]
    public string? Mask { get; set; }
}
