using System.Text.Json.Serialization;

namespace TinyTBS.Game.Modules.Models;

/// <summary>Common fields of any <c>module.json</c> for library scan / install.</summary>
internal sealed class ContentModuleManifestDto
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
}
