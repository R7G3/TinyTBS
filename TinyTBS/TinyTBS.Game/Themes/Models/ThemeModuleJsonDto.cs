using System.Text.Json.Serialization;

namespace TinyTBS.Game.Themes.Models;

internal sealed class ThemeModuleJsonDto
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
    public ThemeModuleContentDto? Content { get; set; }

    [JsonPropertyName("remaps")]
    public Dictionary<string, ThemeSpriteRemapDto>? Remaps { get; set; }
}

internal sealed class ThemeModuleContentDto
{
    [JsonPropertyName("terrainDir")]
    public string? TerrainDir { get; set; }

    [JsonPropertyName("gravestone")]
    public string? Gravestone { get; set; }
}

internal sealed class ThemeSpriteRemapDto
{
    [JsonPropertyName("base")]
    public string? Base { get; set; }

    [JsonPropertyName("mask")]
    public string? Mask { get; set; }
}
