using System.Text.Json.Serialization;

namespace TinyTBS.Game.Units.Models;

internal sealed class UnitModuleJsonDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("content")]
    public UnitModuleContentDto? Content { get; set; }

    [JsonPropertyName("recruit")]
    public UnitModuleRecruitDto? Recruit { get; set; }
}

internal sealed class UnitModuleContentDto
{
    [JsonPropertyName("unitsDir")]
    public string? UnitsDir { get; set; }
}

internal sealed class UnitModuleRecruitDto
{
    [JsonPropertyName("addsToPool")]
    public List<string>? AddsToPool { get; set; }
}
