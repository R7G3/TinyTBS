using System.Text.Json.Serialization;

namespace TinyTBS.Game.Levels.Models;

/// <summary>DTO for the <c>map</c> object — only <c>ref</c> is supported (no embed).</summary>
internal sealed class LevelMapRefDto
{
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }
}
