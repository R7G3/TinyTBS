using System.Text.Json;
using TinyTBS.Game.Levels.Models;
using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Levels;

/// <summary>
/// Parses level.json into domain settings. Map is supplied separately after resolving map.ref.
/// </summary>
public static class LevelJsonParser
{
    private const int DefaultStartingGold = 500;
    private const int DefaultUnitCap = 25;
    private const string DefaultTeamDefeatMode = "allMembers";
    private const string DefaultConditionType = "standard";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static LevelDefinition Parse(
        Stream jsonStream,
        MapDefinition map,
        string mapRef,
        string? sourceDirectory = null,
        string? scenarioModuleRoot = null)
    {
        LevelJsonDto levelDocument;
        try
        {
            levelDocument = JsonSerializer.Deserialize<LevelJsonDto>(jsonStream, JsonOptions)
                ?? throw new LevelLoadException("level.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new LevelLoadException("Failed to parse level.json.", jsonException);
        }

        return FromDocument(levelDocument, map, mapRef, sourceDirectory, scenarioModuleRoot);
    }

    public static LevelDefinition Parse(
        string json,
        MapDefinition map,
        string mapRef,
        string? sourceDirectory = null,
        string? scenarioModuleRoot = null)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return Parse(stream, map, mapRef, sourceDirectory, scenarioModuleRoot);
    }

    /// <summary>Reads and validates <c>map.ref</c> from level.json without loading the map.</summary>
    public static string ReadMapRef(Stream jsonStream)
    {
        LevelJsonDto levelDocument;
        try
        {
            levelDocument = JsonSerializer.Deserialize<LevelJsonDto>(jsonStream, JsonOptions)
                ?? throw new LevelLoadException("level.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new LevelLoadException("Failed to parse level.json.", jsonException);
        }

        return RequireMapRef(levelDocument);
    }

    private static LevelDefinition FromDocument(
        LevelJsonDto levelDocument,
        MapDefinition map,
        string mapRef,
        string? sourceDirectory,
        string? scenarioModuleRoot)
    {
        if (levelDocument.FormatVersion < 1)
            throw new LevelLoadException($"Unsupported formatVersion '{levelDocument.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(levelDocument.Id))
            throw new LevelLoadException("level.json requires non-empty 'id'.");

        var levelId = levelDocument.Id.Trim();
        var modes = ParseModes(levelDocument.Modes);
        var players = ParsePlayers(levelDocument.Players);

        return new LevelDefinition
        {
            FormatVersion = levelDocument.FormatVersion,
            Id = levelId,
            Title = string.IsNullOrWhiteSpace(levelDocument.Title) ? levelId : levelDocument.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(levelDocument.Description)
                ? null
                : levelDocument.Description.Trim(),
            Modes = modes,
            MapRef = mapRef,
            Map = map,
            Players = players,
            DefaultStartingGold = levelDocument.DefaultStartingGold ?? DefaultStartingGold,
            DefaultUnitCap = levelDocument.DefaultUnitCap ?? DefaultUnitCap,
            TeamDefeatMode = string.IsNullOrWhiteSpace(levelDocument.TeamDefeatMode)
                ? DefaultTeamDefeatMode
                : levelDocument.TeamDefeatMode.Trim(),
            Victory = ParseCondition(levelDocument.Victory),
            Defeat = ParseCondition(levelDocument.Defeat),
            Dialogs = ParseDialogs(levelDocument.Dialogs),
            SourceDirectory = sourceDirectory,
            ScenarioModuleRoot = scenarioModuleRoot,
        };
    }

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
            return ["skirmish"];

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
            return new LevelConditionSettings { Type = DefaultConditionType };

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
