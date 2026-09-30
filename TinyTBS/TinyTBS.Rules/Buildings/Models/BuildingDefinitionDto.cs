using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Buildings.Models;

internal sealed class BuildingDefinitionDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("displayNameKey")]
    public string? DisplayNameKey { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("sprites")]
    public BuildingSpritesDto? Sprites { get; set; }

    [JsonPropertyName("income")]
    public int Income { get; set; }

    [JsonPropertyName("defenceBonus")]
    public int DefenceBonus { get; set; }

    [JsonPropertyName("allowsRecruit")]
    public bool AllowsRecruit { get; set; }

    [JsonPropertyName("recruitFromTags")]
    public List<string>? RecruitFromTags { get; set; }

    [JsonPropertyName("heal")]
    public BuildingHealDto? Heal { get; set; }

    [JsonPropertyName("capturable")]
    public bool Capturable { get; set; }

    [JsonPropertyName("destroyable")]
    public bool Destroyable { get; set; }

    [JsonPropertyName("repairable")]
    public bool Repairable { get; set; }

    [JsonPropertyName("ruined")]
    public BuildingRuinedDto? Ruined { get; set; }

    [JsonPropertyName("countsTowardPlayerDefeat")]
    public bool CountsTowardPlayerDefeat { get; set; }
}
