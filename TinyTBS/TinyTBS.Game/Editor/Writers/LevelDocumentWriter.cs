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
        var levelRoot = _files.Combine(scenarioModuleRoot, "Levels", document.Id);
        _files.CreateDirectory(levelRoot);

        var playersMin = Math.Max(1, document.PlayersMin);
        var playersMax = Math.Max(playersMin, document.PlayersMax);
        var defaultSlots = Math.Clamp(document.DefaultSlots, playersMin, playersMax);

        var payload = new LevelJsonDto
        {
            FormatVersion = 1,
            Id = document.Id.Trim(),
            Title = string.IsNullOrWhiteSpace(document.Title) ? document.Id : document.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(document.Description) ? null : document.Description.Trim(),
            Modes = document.Modes.Count > 0 ? document.Modes.ToList() : ["skirmish"],
            Map = new LevelMapRefDto { Ref = document.MapRef.Replace('\\', '/') },
            Players = new LevelPlayersDto
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
            Victory = new LevelConditionDto
            {
                Type = string.IsNullOrWhiteSpace(document.VictoryType) ? "standard" : document.VictoryType.Trim(),
            },
            Defeat = new LevelConditionDto
            {
                Type = string.IsNullOrWhiteSpace(document.DefeatType) ? "standard" : document.DefeatType.Trim(),
            },
        };

        var path = _files.Combine(levelRoot, LevelFolderLoader.LevelJsonFileName);
        _files.WriteAllText(path, JsonSerializer.Serialize(payload, ContentJson.Write) + Environment.NewLine, Encoding.UTF8);
        document.IsDirty = false;
        return levelRoot;
    }
}
