using System.Text;
using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Levels;
using TinyTBS.Game.Levels;
using TinyTBS.Rules;
using TinyTBS.Rules.Levels.Models;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes <c>Levels/{id}/level.json</c> under a scenario module.</summary>
public sealed class LevelDocumentWriter
{
    private readonly IFileSystem _files;

    public LevelDocumentWriter(IFileSystem files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string Write(string scenarioModuleRoot, EditableLevelDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentNullException.ThrowIfNull(document);

        ContentModuleManifestParser.ValidateModuleId(document.Id);
        document.Title = SavedUserText.Or(document.Title, document.Id);
        document.Description = SavedUserText.Optional(document.Description);
        document.MapRef = SavedUserText.Or(document.MapRef, "Maps/map").Replace('\\', '/');
        document.TeamDefeatMode = SavedUserText.Or(document.TeamDefeatMode, TeamDefeatModeIds.AllMembers);
        document.VictoryType = SavedUserText.Or(document.VictoryType, MatchConditionTypes.Standard);
        document.DefeatType = SavedUserText.Or(document.DefeatType, MatchConditionTypes.Standard);
        var modes = SavedUserText.List(document.Modes);
        document.Modes = modes.Count > 0 ? modes : [LevelModeIds.Skirmish];
        ContentModuleManifestParser.ValidateModuleId(document.MapIdFromRef());

        var levelRoot = _files.Combine(scenarioModuleRoot, "Levels", document.Id);
        _files.CreateDirectory(levelRoot);

        var playersMin = Math.Max(1, document.PlayersMin);
        var playersMax = Math.Max(playersMin, document.PlayersMax);
        var defaultSlots = Math.Clamp(document.DefaultSlots, playersMin, playersMax);

        var payload = new LevelJsonDto
        {
            FormatVersion = 1,
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            Modes = document.Modes.ToList(),
            Map = new LevelMapRefDto { Ref = document.MapRef },
            Players = new LevelPlayersDto
            {
                Min = playersMin,
                Max = playersMax,
                DefaultSlots = defaultSlots,
            },
            DefaultStartingGold = Math.Max(0, document.DefaultStartingGold),
            DefaultUnitCap = Math.Max(1, document.DefaultUnitCap),
            TeamDefeatMode = document.TeamDefeatMode,
            Victory = new LevelConditionDto
            {
                Type = document.VictoryType,
            },
            Defeat = new LevelConditionDto
            {
                Type = document.DefeatType,
            },
        };

        var path = _files.Combine(levelRoot, LevelFolderLoader.LevelJsonFileName);
        _files.WriteAllText(path, JsonSerializer.Serialize(payload, ContentJson.Write) + Environment.NewLine, Encoding.UTF8);
        document.IsDirty = false;
        return levelRoot;
    }
}
