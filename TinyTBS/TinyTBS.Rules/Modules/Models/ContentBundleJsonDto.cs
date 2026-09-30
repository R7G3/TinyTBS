using System.Text.Json.Serialization;

namespace TinyTBS.Rules.Modules.Models;

internal sealed class ContentBundleJsonDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("modules")]
    public List<string>? Modules { get; set; }

    [JsonPropertyName("defaults")]
    public ContentBundleDefaultsDto? Defaults { get; set; }
}
