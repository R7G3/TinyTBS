using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Levels;
using TinyTBS.Rules.Levels.Models;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Lists levels under <c>{scenarioModuleRoot}/Levels/{levelId}/level.json</c>.</summary>
public static class ScenarioLevelCatalog
{
    private const int FallbackPlayersMin = 2;
    private const int FallbackPlayersMax = 2;
    private const int FallbackStartingGold = 500;
    private const int FallbackUnitCap = 25;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static IReadOnlyList<ScenarioLevelInfo> ListLevels(
        string scenarioModuleRoot,
        IFileSystem files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentNullException.ThrowIfNull(files);

        var levelsRoot = files.Combine(scenarioModuleRoot, "Levels");
        if (!files.DirectoryExists(levelsRoot))
            return [];

        var results = new List<ScenarioLevelInfo>();
        foreach (var levelFolder in files.EnumerateDirectories(levelsRoot)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var levelJsonPath = files.Combine(levelFolder, LevelFolderLoader.LevelJsonFileName);
            if (!files.Exists(levelJsonPath))
                continue;

            try
            {
                using var stream = files.OpenRead(levelJsonPath);
                var document = JsonSerializer.Deserialize<LevelJsonDto>(stream, JsonOptions);
                if (document is null || string.IsNullOrWhiteSpace(document.Id))
                    continue;

                var levelId = document.Id.Trim();
                var title = string.IsNullOrWhiteSpace(document.Title) ? levelId : document.Title.Trim();
                var modes = (document.Modes ?? [])
                    .Where(mode => !string.IsNullOrWhiteSpace(mode))
                    .Select(mode => mode.Trim())
                    .ToArray();

                var playersMin = document.Players?.Min > 0 ? document.Players.Min : FallbackPlayersMin;
                var playersMax = document.Players?.Max >= playersMin
                    ? document.Players.Max
                    : Math.Max(playersMin, FallbackPlayersMax);
                var defaultSlots = document.Players?.DefaultSlots > 0
                    ? document.Players.DefaultSlots
                    : playersMax;
                defaultSlots = Math.Clamp(defaultSlots, playersMin, playersMax);

                results.Add(new ScenarioLevelInfo
                {
                    LevelId = levelId,
                    Title = title,
                    LevelFolderPath = levelFolder,
                    Modes = modes,
                    PlayersMin = playersMin,
                    PlayersMax = playersMax,
                    PlayersDefaultSlots = defaultSlots,
                    DefaultStartingGold = document.DefaultStartingGold ?? FallbackStartingGold,
                    DefaultUnitCap = document.DefaultUnitCap ?? FallbackUnitCap,
                });
            }
            catch (JsonException)
            {
                // Skip broken level folders; New Game still lists valid ones.
            }
            catch (IOException)
            {
            }
        }

        return results;
    }
}
