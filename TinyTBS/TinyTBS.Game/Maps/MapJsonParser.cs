using System.Text.Json;
using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Maps;

/// <summary>Parses map.json into <see cref="MapDefinition"/>.</summary>
public static class MapJsonParser
{
    private const string DefaultTerrainType = "grass";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static MapDefinition Parse(
        Stream jsonStream,
        string? sourceDirectory = null,
        string? scriptPath = null)
    {
        MapJsonDto mapDocument;
        try
        {
            mapDocument = JsonSerializer.Deserialize<MapJsonDto>(jsonStream, JsonOptions)
                ?? throw new MapLoadException("map.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new MapLoadException("Failed to parse map.json.", jsonException);
        }

        return FromDocument(mapDocument, sourceDirectory, scriptPath);
    }

    public static MapDefinition Parse(
        string json,
        string? sourceDirectory = null,
        string? scriptPath = null)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return Parse(stream, sourceDirectory, scriptPath);
    }

    private static MapDefinition FromDocument(
        MapJsonDto mapDocument,
        string? sourceDirectory,
        string? scriptPath)
    {
        if (mapDocument.FormatVersion < 1)
            throw new MapLoadException($"Unsupported formatVersion '{mapDocument.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(mapDocument.Id))
            throw new MapLoadException("map.json requires non-empty 'id'.");

        if (mapDocument.Width <= 0 || mapDocument.Height <= 0)
        {
            throw new MapLoadException(
                $"Invalid map size {mapDocument.Width}x{mapDocument.Height}.");
        }

        if (mapDocument.Layers is null)
            throw new MapLoadException("map.json requires 'layers'.");

        var surface = ParseSurface(mapDocument.Layers.Surface, mapDocument.Width, mapDocument.Height);
        var buildings = ParseBuildings(mapDocument.Layers.Buildings, mapDocument.Width, mapDocument.Height);
        var units = ParseUnits(mapDocument.Layers.Units, mapDocument.Width, mapDocument.Height);
        var memorials = ParseMemorials(mapDocument.Layers.Memorials, mapDocument.Width, mapDocument.Height);

        var mapId = mapDocument.Id.Trim();
        return new MapDefinition
        {
            FormatVersion = mapDocument.FormatVersion,
            Id = mapId,
            Title = string.IsNullOrWhiteSpace(mapDocument.Title) ? mapId : mapDocument.Title.Trim(),
            Width = mapDocument.Width,
            Height = mapDocument.Height,
            Surface = surface,
            Buildings = buildings,
            Units = units,
            Memorials = memorials,
            ScriptPath = scriptPath,
            SourceDirectory = sourceDirectory,
        };
    }

    private static string[,] ParseSurface(JsonElement surfaceLayer, int width, int height)
    {
        var terrainGrid = CreateDefaultTerrainGrid(width, height);

        if (surfaceLayer.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return terrainGrid;

        if (surfaceLayer.ValueKind != JsonValueKind.Array)
            throw new MapLoadException("layers.surface must be a JSON array.");

        var entryCount = surfaceLayer.GetArrayLength();
        if (entryCount == 0)
            return terrainGrid;

        // Dense: width*height strings (row-major, y then x).
        if (surfaceLayer[0].ValueKind == JsonValueKind.String)
            return ParseDenseSurface(surfaceLayer, terrainGrid, width, height, entryCount);

        // Sparse: { "x", "y", "type" } — unspecified cells keep the default terrain.
        return ParseSparseSurface(surfaceLayer, terrainGrid, width, height, entryCount);
    }

    private static string[,] CreateDefaultTerrainGrid(int width, int height)
    {
        var terrainGrid = new string[width, height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                terrainGrid[x, y] = DefaultTerrainType;
        }

        return terrainGrid;
    }

    private static string[,] ParseDenseSurface(
        JsonElement surfaceLayer,
        string[,] terrainGrid,
        int width,
        int height,
        int entryCount)
    {
        if (entryCount != width * height)
        {
            throw new MapLoadException(
                $"Dense surface length {entryCount} does not match width*height ({width * height}).");
        }

        for (var i = 0; i < entryCount; i++)
        {
            var terrainType = surfaceLayer[i].GetString();
            if (string.IsNullOrWhiteSpace(terrainType))
                throw new MapLoadException($"surface[{i}] is empty.");

            var x = i % width;
            var y = i / width;
            terrainGrid[x, y] = terrainType.Trim().ToLowerInvariant();
        }

        return terrainGrid;
    }

    private static string[,] ParseSparseSurface(
        JsonElement surfaceLayer,
        string[,] terrainGrid,
        int width,
        int height,
        int entryCount)
    {
        for (var i = 0; i < entryCount; i++)
        {
            var cellObject = surfaceLayer[i];
            if (cellObject.ValueKind != JsonValueKind.Object)
                throw new MapLoadException($"surface[{i}] must be a string or object.");

            if (!cellObject.TryGetProperty("x", out var xProperty)
                || !cellObject.TryGetProperty("y", out var yProperty)
                || !cellObject.TryGetProperty("type", out var typeProperty))
            {
                throw new MapLoadException($"surface[{i}] requires x, y, type.");
            }

            var x = xProperty.GetInt32();
            var y = yProperty.GetInt32();
            var terrainType = typeProperty.GetString();
            EnsureInBounds(x, y, width, height, $"surface[{i}]");

            if (string.IsNullOrWhiteSpace(terrainType))
                throw new MapLoadException($"surface[{i}].type is empty.");

            terrainGrid[x, y] = terrainType.Trim().ToLowerInvariant();
        }

        return terrainGrid;
    }

    private static List<MapBuildingPlacement> ParseBuildings(
        List<MapBuildingDto>? buildingEntries,
        int width,
        int height)
    {
        if (buildingEntries is null || buildingEntries.Count == 0)
            return [];

        var placements = new List<MapBuildingPlacement>(buildingEntries.Count);
        for (var i = 0; i < buildingEntries.Count; i++)
        {
            var buildingEntry = buildingEntries[i];
            EnsureInBounds(buildingEntry.X, buildingEntry.Y, width, height, $"buildings[{i}]");
            if (string.IsNullOrWhiteSpace(buildingEntry.Type))
                throw new MapLoadException($"buildings[{i}].type is required.");

            placements.Add(new MapBuildingPlacement
            {
                Type = ContentId.Parse(buildingEntry.Type),
                X = buildingEntry.X,
                Y = buildingEntry.Y,
                Slot = buildingEntry.Slot,
                State = buildingEntry.State,
            });
        }

        return placements;
    }

    private static List<MapUnitPlacement> ParseUnits(
        List<MapUnitDto>? unitEntries,
        int width,
        int height)
    {
        if (unitEntries is null || unitEntries.Count == 0)
            return [];

        var placements = new List<MapUnitPlacement>(unitEntries.Count);
        for (var i = 0; i < unitEntries.Count; i++)
        {
            var unitEntry = unitEntries[i];
            EnsureInBounds(unitEntry.X, unitEntry.Y, width, height, $"units[{i}]");
            if (string.IsNullOrWhiteSpace(unitEntry.Type))
                throw new MapLoadException($"units[{i}].type is required.");

            placements.Add(new MapUnitPlacement
            {
                Type = ContentId.Parse(unitEntry.Type),
                X = unitEntry.X,
                Y = unitEntry.Y,
                Slot = unitEntry.Slot,
                Hp = unitEntry.Hp,
                Xp = unitEntry.Xp,
            });
        }

        return placements;
    }

    private static List<MapMemorialPlacement> ParseMemorials(
        List<MapMemorialDto>? memorialEntries,
        int width,
        int height)
    {
        if (memorialEntries is null || memorialEntries.Count == 0)
            return [];

        var placements = new List<MapMemorialPlacement>(memorialEntries.Count);
        for (var i = 0; i < memorialEntries.Count; i++)
        {
            var memorialEntry = memorialEntries[i];
            EnsureInBounds(memorialEntry.X, memorialEntry.Y, width, height, $"memorials[{i}]");
            placements.Add(new MapMemorialPlacement { X = memorialEntry.X, Y = memorialEntry.Y });
        }

        return placements;
    }

    private static void EnsureInBounds(int x, int y, int width, int height, string entryLabel)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            throw new MapLoadException($"{entryLabel} cell ({x},{y}) is outside {width}x{height}.");
    }
}
