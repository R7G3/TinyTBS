using System.Text.Json;
using System.Text.Json.Serialization;

namespace TinyTBS.Game.Maps.Models;

/// <summary>DTO for the <c>layers</c> object in map.json.</summary>
internal sealed class MapLayersDto
{
    [JsonPropertyName("surface")]
    public JsonElement Surface { get; set; }

    [JsonPropertyName("buildings")]
    public List<MapBuildingDto>? Buildings { get; set; }

    [JsonPropertyName("units")]
    public List<MapUnitDto>? Units { get; set; }

    [JsonPropertyName("memorials")]
    public List<MapMemorialDto>? Memorials { get; set; }
}
