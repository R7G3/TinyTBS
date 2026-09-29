using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Levels;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes a minimal skirmish <c>Levels/{id}/level.json</c> pointing at <c>Maps/{id}</c>.</summary>
public sealed class LevelStubWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileContentProvider _files;

    public LevelStubWriter(IFileContentProvider files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string WriteSkirmishStub(
        string scenarioModuleRoot,
        string levelId,
        string title,
        string mapId,
        int playersMin = 2,
        int playersMax = 2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(levelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);

        ContentModuleManifestParser.ValidateModuleId(levelId.Trim());
        ContentModuleManifestParser.ValidateModuleId(mapId.Trim());

        var levelRoot = _files.Combine(scenarioModuleRoot, "Levels", levelId.Trim());
        Directory.CreateDirectory(levelRoot);

        var payload = new LevelStubWriteDto
        {
            FormatVersion = 1,
            Id = levelId.Trim(),
            Title = title.Trim(),
            Description = "Created in TinyTBS Editor.",
            Modes = ["skirmish"],
            Map = new LevelMapRefWriteDto { Ref = "Maps/" + mapId.Trim() },
            Players = new LevelPlayersWriteDto
            {
                Min = playersMin,
                Max = Math.Max(playersMin, playersMax),
                DefaultSlots = Math.Max(playersMin, playersMax),
            },
            DefaultStartingGold = 500,
            DefaultUnitCap = 25,
            TeamDefeatMode = "allMembers",
            Victory = new LevelConditionWriteDto { Type = "standard" },
            Defeat = new LevelConditionWriteDto { Type = "standard" },
        };

        var path = _files.Combine(levelRoot, LevelFolderLoader.LevelJsonFileName);
        File.WriteAllText(path, JsonSerializer.Serialize(payload, WriteOptions) + Environment.NewLine, Encoding.UTF8);
        return levelRoot;
    }

    private sealed class LevelStubWriteDto
    {
        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("modes")]
        public List<string> Modes { get; set; } = [];

        [JsonPropertyName("map")]
        public LevelMapRefWriteDto Map { get; set; } = new();

        [JsonPropertyName("players")]
        public LevelPlayersWriteDto Players { get; set; } = new();

        [JsonPropertyName("defaultStartingGold")]
        public int DefaultStartingGold { get; set; }

        [JsonPropertyName("defaultUnitCap")]
        public int DefaultUnitCap { get; set; }

        [JsonPropertyName("teamDefeatMode")]
        public string TeamDefeatMode { get; set; } = string.Empty;

        [JsonPropertyName("victory")]
        public LevelConditionWriteDto Victory { get; set; } = new();

        [JsonPropertyName("defeat")]
        public LevelConditionWriteDto Defeat { get; set; } = new();
    }

    private sealed class LevelMapRefWriteDto
    {
        [JsonPropertyName("ref")]
        public string Ref { get; set; } = string.Empty;
    }

    private sealed class LevelPlayersWriteDto
    {
        [JsonPropertyName("min")]
        public int Min { get; set; }

        [JsonPropertyName("max")]
        public int Max { get; set; }

        [JsonPropertyName("defaultSlots")]
        public int DefaultSlots { get; set; }
    }

    private sealed class LevelConditionWriteDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}
