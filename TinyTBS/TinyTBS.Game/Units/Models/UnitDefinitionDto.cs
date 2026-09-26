using System.Text.Json.Serialization;

namespace TinyTBS.Game.Units.Models;

internal sealed class UnitDefinitionDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("displayNameKey")]
    public string? DisplayNameKey { get; set; }

    [JsonPropertyName("movementClass")]
    public string? MovementClass { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("recruitable")]
    public bool? Recruitable { get; set; }

    [JsonPropertyName("attack")]
    public int Attack { get; set; }

    [JsonPropertyName("defence")]
    public int Defence { get; set; }

    [JsonPropertyName("maxHealth")]
    public int MaxHealth { get; set; }

    [JsonPropertyName("attackRangeMin")]
    public int AttackRangeMin { get; set; }

    [JsonPropertyName("attackRangeMax")]
    public int AttackRangeMax { get; set; }

    [JsonPropertyName("speed")]
    public int Speed { get; set; }

    [JsonPropertyName("cost")]
    public int Cost { get; set; }

    [JsonPropertyName("abilities")]
    public List<UnitAbilityDto>? Abilities { get; set; }

    [JsonPropertyName("leavesGravestone")]
    public bool? LeavesGravestone { get; set; }

    [JsonPropertyName("sprites")]
    public UnitSpritesDto? Sprites { get; set; }
}

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

internal sealed class UnitSpritesDto
{
    [JsonPropertyName("base")]
    public string? Base { get; set; }

    [JsonPropertyName("mask")]
    public string? Mask { get; set; }
}
