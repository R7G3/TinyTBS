using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Levels;
using TinyTBS.Game.Levels.Models;

namespace TinyTBS.Game.Editor.Levels;

/// <summary>Mutable level.json fields for the Editor (map not loaded).</summary>
public sealed class EditableLevelDocument
{
    public string Id { get; set; } = "level";

    public string Title { get; set; } = "level";

    public string? Description { get; set; }

    public List<string> Modes { get; set; } = ["skirmish"];

    /// <summary>e.g. <c>Maps/demo</c>.</summary>
    public string MapRef { get; set; } = "Maps/map";

    public int PlayersMin { get; set; } = 2;

    public int PlayersMax { get; set; } = 2;

    public int DefaultSlots { get; set; } = 2;

    public int DefaultStartingGold { get; set; } = 500;

    public int DefaultUnitCap { get; set; } = 25;

    public string TeamDefeatMode { get; set; } = "allMembers";

    public string VictoryType { get; set; } = "standard";

    public string DefeatType { get; set; } = "standard";

    public bool IsDirty { get; set; }

    public static EditableLevelDocument CreateDefault(string levelId, string title, string mapId)
    {
        var id = string.IsNullOrWhiteSpace(levelId) ? "level" : levelId.Trim();
        var map = string.IsNullOrWhiteSpace(mapId) ? "map" : mapId.Trim();
        return new EditableLevelDocument
        {
            Id = id,
            Title = string.IsNullOrWhiteSpace(title) ? id : title.Trim(),
            Description = "Created in TinyTBS Editor.",
            Modes = ["skirmish"],
            MapRef = "Maps/" + map,
            PlayersMin = 2,
            PlayersMax = 2,
            DefaultSlots = 2,
            DefaultStartingGold = 500,
            DefaultUnitCap = 25,
            TeamDefeatMode = "allMembers",
            VictoryType = "standard",
            DefeatType = "standard",
            IsDirty = true,
        };
    }

    public static EditableLevelDocument Load(string levelDirectory, IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelDirectory);
        ArgumentNullException.ThrowIfNull(files);

        var path = files.Combine(levelDirectory, LevelFolderLoader.LevelJsonFileName);
        if (!files.Exists(path))
            throw new LevelLoadException($"Missing {LevelFolderLoader.LevelJsonFileName} in {levelDirectory}");

        using var stream = files.OpenRead(path);
        LevelJsonDto? document;
        try
        {
            document = JsonSerializer.Deserialize<LevelJsonDto>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        }
        catch (JsonException exception)
        {
            throw new LevelLoadException("Failed to parse level.json.", exception);
        }

        if (document is null)
            throw new LevelLoadException("level.json deserialized to null.");

        var id = string.IsNullOrWhiteSpace(document.Id) ? "level" : document.Id.Trim();
        var mapRef = document.Map?.Ref?.Trim() ?? "Maps/map";
        var players = document.Players;
        return new EditableLevelDocument
        {
            Id = id,
            Title = string.IsNullOrWhiteSpace(document.Title) ? id : document.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(document.Description) ? null : document.Description.Trim(),
            Modes = document.Modes is { Count: > 0 }
                ? document.Modes.Where(mode => !string.IsNullOrWhiteSpace(mode)).Select(mode => mode.Trim()).ToList()
                : ["skirmish"],
            MapRef = mapRef.Replace('\\', '/'),
            PlayersMin = players?.Min ?? 2,
            PlayersMax = players?.Max ?? Math.Max(2, players?.Min ?? 2),
            DefaultSlots = players?.DefaultSlots ?? players?.Max ?? 2,
            DefaultStartingGold = document.DefaultStartingGold ?? 500,
            DefaultUnitCap = document.DefaultUnitCap ?? 25,
            TeamDefeatMode = string.IsNullOrWhiteSpace(document.TeamDefeatMode)
                ? "allMembers"
                : document.TeamDefeatMode.Trim(),
            VictoryType = string.IsNullOrWhiteSpace(document.Victory?.Type)
                ? "standard"
                : document.Victory!.Type!.Trim(),
            DefeatType = string.IsNullOrWhiteSpace(document.Defeat?.Type)
                ? "standard"
                : document.Defeat!.Type!.Trim(),
            IsDirty = false,
        };
    }

    public string MapIdFromRef()
    {
        var mapRef = MapRef.Replace('\\', '/').Trim();
        const string prefix = "Maps/";
        if (mapRef.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return mapRef[prefix.Length..].Trim('/');
        return mapRef.Trim('/');
    }

    public void SetMapId(string mapId)
    {
        var id = string.IsNullOrWhiteSpace(mapId) ? "map" : mapId.Trim();
        MapRef = "Maps/" + id;
        IsDirty = true;
    }

    public bool HasMode(string mode) =>
        Modes.Any(entry => string.Equals(entry, mode, StringComparison.OrdinalIgnoreCase));

    public void SetMode(string mode, bool enabled)
    {
        var normalized = mode.Trim();
        Modes.RemoveAll(entry => string.Equals(entry, normalized, StringComparison.OrdinalIgnoreCase));
        if (enabled)
            Modes.Add(normalized);
        if (Modes.Count == 0)
            Modes.Add("skirmish");
        IsDirty = true;
    }
}
