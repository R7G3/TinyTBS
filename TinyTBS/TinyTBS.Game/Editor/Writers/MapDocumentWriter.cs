using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Maps;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes <c>Maps/{id}/map.json</c> (+ optional script) under a scenario module.</summary>
public sealed class MapDocumentWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileContentProvider _files;

    public MapDocumentWriter(IFileContentProvider files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string Write(string scenarioModuleRoot, EditableMapDocument document, string? scriptText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentNullException.ThrowIfNull(document);

        ContentModuleManifestParser.ValidateModuleId(document.Id);
        var mapRoot = _files.Combine(scenarioModuleRoot, "Maps", document.Id);
        Directory.CreateDirectory(mapRoot);

        var surface = new string[document.Width * document.Height];
        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
                surface[y * document.Width + x] = document.Surface[x, y];
        }

        var payload = new MapWriteDto
        {
            FormatVersion = 1,
            Id = document.Id,
            Title = document.Title,
            Width = document.Width,
            Height = document.Height,
            Layers = new MapLayersWriteDto
            {
                Surface = surface,
                Buildings = document.Buildings.Select(building => new MapBuildingWriteDto
                {
                    Type = building.Type.Full,
                    X = building.X,
                    Y = building.Y,
                    Slot = building.Slot,
                    State = building.State,
                }).ToList(),
                Units = document.Units.Select(unit => new MapUnitWriteDto
                {
                    Type = unit.Type.Full,
                    X = unit.X,
                    Y = unit.Y,
                    Slot = unit.Slot,
                    Hp = unit.Hp,
                    Xp = unit.Xp,
                }).ToList(),
                Gravestones = document.Gravestones.Select(grave => new MapGravestoneWriteDto
                {
                    X = grave.X,
                    Y = grave.Y,
                }).ToList(),
            },
        };

        var mapJsonPath = _files.Combine(mapRoot, MapFolderLoader.MapJsonFileName);
        var json = JsonSerializer.Serialize(payload, WriteOptions);
        File.WriteAllText(mapJsonPath, json + Environment.NewLine, Encoding.UTF8);

        var scriptPath = _files.Combine(mapRoot, "script.cs");
        if (scriptText is not null)
        {
            File.WriteAllText(scriptPath, scriptText, Encoding.UTF8);
        }
        else if (!File.Exists(scriptPath))
        {
            File.WriteAllText(scriptPath, MapScriptTemplates.EmptyHooks, Encoding.UTF8);
        }

        document.IsDirty = false;
        return mapRoot;
    }

    private sealed class MapWriteDto
    {
        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("layers")]
        public MapLayersWriteDto Layers { get; set; } = new();
    }

    private sealed class MapLayersWriteDto
    {
        [JsonPropertyName("surface")]
        public string[] Surface { get; set; } = [];

        [JsonPropertyName("buildings")]
        public List<MapBuildingWriteDto> Buildings { get; set; } = [];

        [JsonPropertyName("units")]
        public List<MapUnitWriteDto> Units { get; set; } = [];

        [JsonPropertyName("gravestones")]
        public List<MapGravestoneWriteDto> Gravestones { get; set; } = [];
    }

    private sealed class MapBuildingWriteDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("slot")]
        public int? Slot { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }
    }

    private sealed class MapUnitWriteDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("slot")]
        public int Slot { get; set; }

        [JsonPropertyName("hp")]
        public int? Hp { get; set; }

        [JsonPropertyName("xp")]
        public int? Xp { get; set; }
    }

    private sealed class MapGravestoneWriteDto
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }
}
