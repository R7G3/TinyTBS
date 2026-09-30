using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Maps;
using TinyTBS.Game.Maps.Models;
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

    private readonly IFileSystem _files;

    public MapDocumentWriter(IFileSystem files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string Write(string scenarioModuleRoot, EditableMapDocument document, string? scriptText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentNullException.ThrowIfNull(document);

        ContentModuleManifestParser.ValidateModuleId(document.Id);
        var mapRoot = _files.Combine(scenarioModuleRoot, "Maps", document.Id);
        _files.CreateDirectory(mapRoot);

        var surface = new string[document.Width * document.Height];
        for (var y = 0; y < document.Height; y++)
        {
            for (var x = 0; x < document.Width; x++)
                surface[y * document.Width + x] = document.Surface[x, y];
        }

        var payload = new MapJsonDto
        {
            FormatVersion = 1,
            Id = document.Id,
            Title = document.Title,
            Width = document.Width,
            Height = document.Height,
            Layers = new MapLayersDto
            {
                Surface = JsonSerializer.SerializeToElement(surface),
                Buildings = document.Buildings.Select(building => new MapBuildingDto
                {
                    Type = building.Type.Full,
                    X = building.X,
                    Y = building.Y,
                    Slot = building.Slot,
                    State = building.State,
                }).ToList(),
                Units = document.Units.Select(unit => new MapUnitDto
                {
                    Type = unit.Type.Full,
                    X = unit.X,
                    Y = unit.Y,
                    Slot = unit.Slot,
                    Hp = unit.Hp,
                    Xp = unit.Xp,
                }).ToList(),
                Gravestones = document.Gravestones.Select(grave => new MapGravestoneDto
                {
                    X = grave.X,
                    Y = grave.Y,
                }).ToList(),
            },
        };

        var mapJsonPath = _files.Combine(mapRoot, MapFolderLoader.MapJsonFileName);
        var json = JsonSerializer.Serialize(payload, WriteOptions);
        _files.WriteAllText(mapJsonPath, json + Environment.NewLine, Encoding.UTF8);

        var scriptPath = _files.Combine(mapRoot, "script.cs");
        if (scriptText is not null)
        {
            _files.WriteAllText(scriptPath, scriptText, Encoding.UTF8);
        }
        else if (!_files.Exists(scriptPath))
        {
            _files.WriteAllText(scriptPath, MapScriptTemplates.EmptyHooks, Encoding.UTF8);
        }

        document.IsDirty = false;
        return mapRoot;
    }
}
