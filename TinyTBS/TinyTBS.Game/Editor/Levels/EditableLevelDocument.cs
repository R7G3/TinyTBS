using TinyTBS.Engine.IO;
using TinyTBS.Game.Levels;

namespace TinyTBS.Game.Editor.Levels;

/// <summary>Mutable level.json fields for the Editor (map not loaded).</summary>
public sealed class EditableLevelDocument
{
    public string Id { get; set; } = "level";

    public string Title { get; set; } = "level";

    public string? Description { get; set; }

    public List<string> Modes { get; set; } = [LevelModeIds.Skirmish];

    /// <summary>e.g. <c>Maps/demo</c>.</summary>
    public string MapRef { get; set; } = "Maps/map";

    public int PlayersMin { get; set; } = 2;

    public int PlayersMax { get; set; } = 2;

    public int DefaultSlots { get; set; } = 2;

    public int DefaultStartingGold { get; set; } = 500;

    public int DefaultUnitCap { get; set; } = 25;

    public string TeamDefeatMode { get; set; } = TeamDefeatModeIds.AllMembers;

    public string VictoryType { get; set; } = MatchConditionTypes.Standard;

    public string DefeatType { get; set; } = MatchConditionTypes.Standard;

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
            Modes = [LevelModeIds.Skirmish],
            MapRef = "Maps/" + map,
            PlayersMin = 2,
            PlayersMax = 2,
            DefaultSlots = 2,
            DefaultStartingGold = 500,
            DefaultUnitCap = 25,
            TeamDefeatMode = TeamDefeatModeIds.AllMembers,
            VictoryType = MatchConditionTypes.Standard,
            DefeatType = MatchConditionTypes.Standard,
            IsDirty = true,
        };
    }

    public static EditableLevelDocument Load(string levelDirectory, IFileSystem files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelDirectory);
        ArgumentNullException.ThrowIfNull(files);

        var path = files.Combine(levelDirectory, LevelFolderLoader.LevelJsonFileName);
        if (!files.Exists(path))
            throw new LevelLoadException($"Missing {LevelFolderLoader.LevelJsonFileName} in {levelDirectory}");

        using var stream = files.OpenRead(path);
        var document = LevelJsonParser.Read(stream);
        return new EditableLevelDocument
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            Modes = document.Modes.ToList(),
            MapRef = document.MapRef.Replace('\\', '/'),
            PlayersMin = document.Players.Min,
            PlayersMax = document.Players.Max,
            DefaultSlots = document.Players.DefaultSlots,
            DefaultStartingGold = document.DefaultStartingGold,
            DefaultUnitCap = document.DefaultUnitCap,
            TeamDefeatMode = document.TeamDefeatMode,
            VictoryType = document.Victory.Type,
            DefeatType = document.Defeat.Type,
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
            Modes.Add(LevelModeIds.Skirmish);
        IsDirty = true;
    }
}
