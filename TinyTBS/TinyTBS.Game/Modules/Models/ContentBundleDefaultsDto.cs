using System.Text.Json.Serialization;

namespace TinyTBS.Game.Modules.Models;

internal sealed class ContentBundleDefaultsDto
{
    [JsonPropertyName("scenario")]
    public string? Scenario { get; set; }

    [JsonPropertyName("units")]
    public List<string>? Units { get; set; }

    [JsonPropertyName("buildings")]
    public List<string>? Buildings { get; set; }

    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
}
