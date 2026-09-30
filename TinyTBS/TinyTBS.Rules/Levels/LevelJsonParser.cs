using System.Text.Json;
using TinyTBS.Rules.Levels.Models;
using TinyTBS.Rules.Maps.Models;
using TinyTBS.Rules.Match;

namespace TinyTBS.Rules.Levels;

/// <summary>
/// Parses level.json into domain settings. Map is supplied separately after resolving map.ref.
/// </summary>
public static class LevelJsonParser
{
    private const int DefaultStartingGold = 500;
    private const int DefaultUnitCap = 25;

    public static LevelFileDefinition Read(Stream jsonStream)
    {
        ArgumentNullException.ThrowIfNull(jsonStream);

        LevelJsonDto levelDocument;
        try
        {
            levelDocument = JsonSerializer.Deserialize<LevelJsonDto>(jsonStream, ContentJson.Read)
                ?? throw new LevelLoadException("level.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new LevelLoadException("Failed to parse level.json.", jsonException);
        }

        if (levelDocument.FormatVersion < 1)
            throw new LevelLoadException($"Unsupported formatVersion '{levelDocument.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(levelDocument.Id))
            throw new LevelLoadException("level.json requires non-empty 'id'.");

        var levelId = levelDocument.Id.Trim();
        return new LevelFileDefinition
        {
            FormatVersion = levelDocument.FormatVersion,
            Id = levelId,
            Title = string.IsNullOrWhiteSpace(levelDocument.Title) ? levelId : levelDocument.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(levelDocument.Description)
                ? null
                : levelDocument.Description.Trim(),
            Modes = ParseModes(levelDocument.Modes),
            MapRef = RequireMapRef(levelDocument),
            Players = ParsePlayers(levelDocument.Players),
            DefaultStartingGold = levelDocument.DefaultStartingGold ?? DefaultStartingGold,
            DefaultUnitCap = levelDocument.DefaultUnitCap ?? DefaultUnitCap,
            TeamDefeatMode = string.IsNullOrWhiteSpace(levelDocument.TeamDefeatMode)
                ? TeamDefeatModeIds.AllMembers
                : levelDocument.TeamDefeatMode.Trim(),
            Victory = ParseCondition(levelDocument.Victory),
            Defeat = ParseCondition(levelDocument.Defeat),
            Dialogs = ParseDialogs(levelDocument.Dialogs),
        };
    }

    public static LevelDefinition Parse(
        Stream jsonStream,
        MapDefinition map,
        string mapRef,
        string? sourceDirectory = null,
        string? scenarioModuleRoot = null)
    {
        ArgumentNullException.ThrowIfNull(jsonStream);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapRef);

        var file = Read(jsonStream);
        return new LevelDefinition
        {
            FormatVersion = file.FormatVersion,
            Id = file.Id,
            Title = file.Title,
            Description = file.Description,
            Modes = file.Modes,
            MapRef = mapRef,
            Map = map,
            Players = file.Players,
            DefaultStartingGold = file.DefaultStartingGold,
            DefaultUnitCap = file.DefaultUnitCap,
            TeamDefeatMode = file.TeamDefeatMode,
            Victory = file.Victory,
            Defeat = file.Defeat,
            Dialogs = file.Dialogs,
            SourceDirectory = sourceDirectory,
            ScenarioModuleRoot = scenarioModuleRoot,
        };
    }

    /// <summary>Reads and validates <c>map.ref</c> from level.json without loading the map.</summary>
    public static string ReadMapRef(Stream jsonStream) => Read(jsonStream).MapRef;

    internal static string RequireMapRef(LevelJsonDto levelDocument)
    {
        if (levelDocument.Map is null)
            throw new LevelLoadException("level.json requires 'map' with a 'ref' (embed is not supported).");

        if (string.IsNullOrWhiteSpace(levelDocument.Map.Ref))
            throw new LevelLoadException("level.json map.ref is required.");

        return levelDocument.Map.Ref.Trim();
    }

    private static IReadOnlyList<string> ParseModes(List<string>? modes)
    {
        if (modes is null || modes.Count == 0)
            return [LevelModeIds.Skirmish];

        var parsed = new List<string>(modes.Count);
        for (var i = 0; i < modes.Count; i++)
        {
            var mode = modes[i];
            if (string.IsNullOrWhiteSpace(mode))
                throw new LevelLoadException($"modes[{i}] is empty.");
            parsed.Add(mode.Trim());
        }

        return parsed;
    }

    private static LevelPlayersSettings ParsePlayers(LevelPlayersDto? players)
    {
        if (players is null)
        {
            return new LevelPlayersSettings
            {
                Min = 2,
                Max = 2,
                DefaultSlots = 2,
            };
        }

        if (players.Min < 1)
            throw new LevelLoadException($"players.min must be >= 1 (got {players.Min}).");
        if (players.Max < players.Min)
        {
            throw new LevelLoadException(
                $"players.max ({players.Max}) must be >= players.min ({players.Min}).");
        }

        var defaultSlots = players.DefaultSlots > 0 ? players.DefaultSlots : players.Max;
        if (defaultSlots < players.Min || defaultSlots > players.Max)
        {
            throw new LevelLoadException(
                $"players.defaultSlots ({defaultSlots}) must be between min and max.");
        }

        return new LevelPlayersSettings
        {
            Min = players.Min,
            Max = players.Max,
            DefaultSlots = defaultSlots,
        };
    }

    private static LevelConditionSettings ParseCondition(LevelConditionDto? condition)
    {
        if (condition is null || string.IsNullOrWhiteSpace(condition.Type))
            return new LevelConditionSettings { Type = MatchConditionTypes.Standard };

        return new LevelConditionSettings { Type = condition.Type.Trim() };
    }

    private static LevelDialogsSettings? ParseDialogs(LevelDialogsDto? dialogs)
    {
        if (dialogs is null)
            return null;

        return new LevelDialogsSettings
        {
            Start = string.IsNullOrWhiteSpace(dialogs.Start) ? null : dialogs.Start.Trim(),
            End = string.IsNullOrWhiteSpace(dialogs.End) ? null : dialogs.End.Trim(),
        };
    }
}
