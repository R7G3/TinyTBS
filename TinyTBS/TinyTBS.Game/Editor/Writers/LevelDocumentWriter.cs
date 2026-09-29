using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Levels;
using TinyTBS.Game.Levels;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes <c>Levels/{id}/level.json</c> under a scenario module.</summary>
public sealed class LevelDocumentWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileContentProvider _files;

    public LevelDocumentWriter(IFileContentProvider files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string Write(string scenarioModuleRoot, EditableLevelDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentNullException.ThrowIfNull(document);

        ContentModuleManifestParser.ValidateModuleId(document.Id);
        var levelRoot = _files.Combine(scenarioModuleRoot, "Levels", document.Id);
        Directory.CreateDirectory(levelRoot);

        var playersMin = Math.Max(1, document.PlayersMin);
        var playersMax = Math.Max(playersMin, document.PlayersMax);
        var defaultSlots = Math.Clamp(document.DefaultSlots, playersMin, playersMax);

        var payload = new LevelWriteDto
        {
            FormatVersion = 1,
            Id = document.Id.Trim(),
            Title = string.IsNullOrWhiteSpace(document.Title) ? document.Id : document.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(document.Description) ? null : document.Description.Trim(),
            Modes = document.Modes.Count > 0 ? document.Modes.ToList() : ["skirmish"],
            Map = new LevelMapRefWriteDto { Ref = document.MapRef.Replace('\\', '/') },
            Players = new LevelPlayersWriteDto
            {
                Min = playersMin,
                Max = playersMax,
                DefaultSlots = defaultSlots,
            },
            DefaultStartingGold = Math.Max(0, document.DefaultStartingGold),
            DefaultUnitCap = Math.Max(1, document.DefaultUnitCap),
            TeamDefeatMode = string.IsNullOrWhiteSpace(document.TeamDefeatMode)
                ? "allMembers"
                : document.TeamDefeatMode.Trim(),
            Victory = new LevelConditionWriteDto
            {
                Type = string.IsNullOrWhiteSpace(document.VictoryType) ? "standard" : document.VictoryType.Trim(),
            },
            Defeat = new LevelConditionWriteDto
            {
                Type = string.IsNullOrWhiteSpace(document.DefeatType) ? "standard" : document.DefeatType.Trim(),
            },
        };

        var path = _files.Combine(levelRoot, LevelFolderLoader.LevelJsonFileName);
        File.WriteAllText(path, JsonSerializer.Serialize(payload, WriteOptions) + Environment.NewLine, Encoding.UTF8);
        document.IsDirty = false;
        return levelRoot;
    }

    private sealed class LevelWriteDto
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
