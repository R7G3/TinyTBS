using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns.Models;

namespace TinyTBS.Game.Campaigns;

/// <summary>Parses and loads <c>Campaign/campaign.json</c>.</summary>
public static class CampaignLoader
{
    public const string DefaultManifestRelativePath = "Campaign/campaign.json";
    public const string DefaultScriptRelativePath = "Campaign/script.cs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static CampaignDefinition? TryLoadFromScenario(
        string moduleRootPath,
        string? campaignRelativePath,
        IFileSystem files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRootPath);
        ArgumentNullException.ThrowIfNull(files);

        var relative = string.IsNullOrWhiteSpace(campaignRelativePath)
            ? DefaultManifestRelativePath
            : campaignRelativePath.Replace('\\', '/');

        var manifestPath = files.Combine(moduleRootPath, relative.Split('/'));
        if (!files.Exists(manifestPath))
            return null;

        using var stream = files.OpenRead(manifestPath);
        return Parse(stream, moduleRootPath, relative, files);
    }

    public static CampaignDefinition Parse(
        Stream jsonStream,
        string moduleRootPath,
        string manifestRelativePath,
        IFileSystem files)
    {
        ArgumentNullException.ThrowIfNull(jsonStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRootPath);
        ArgumentNullException.ThrowIfNull(files);

        CampaignJsonDto document;
        try
        {
            document = JsonSerializer.Deserialize<CampaignJsonDto>(jsonStream, JsonOptions)
                ?? throw new MatchContentCompositionException("campaign.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new MatchContentCompositionException("Failed to parse campaign.json.", jsonException);
        }

        if (document.FormatVersion < 1)
            throw new MatchContentCompositionException($"Unsupported campaign formatVersion '{document.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(document.Id))
            throw new MatchContentCompositionException("campaign.json requires non-empty 'id'.");

        var chapters = new List<CampaignChapterDefinition>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (document.Levels is not null)
        {
            foreach (var entry in document.Levels)
            {
                if (entry is null || string.IsNullOrWhiteSpace(entry.LevelId))
                    continue;

                var levelId = entry.LevelId;
                if (!seen.Add(levelId))
                    throw new MatchContentCompositionException($"Duplicate campaign levelId '{levelId}'.");

                var path = string.IsNullOrWhiteSpace(entry.Path)
                    ? $"Levels/{levelId}"
                    : entry.Path.Replace('\\', '/');

                chapters.Add(new CampaignChapterDefinition { LevelId = levelId, Path = path });
            }
        }

        if (chapters.Count == 0)
            throw new MatchContentCompositionException($"Campaign '{document.Id}' has no levels.");

        var scriptPath = files.Combine(moduleRootPath, DefaultScriptRelativePath.Split('/'));
        if (!files.Exists(scriptPath))
            scriptPath = null;

        var campaignId = document.Id;
        var title = string.IsNullOrWhiteSpace(document.Title) ? campaignId : document.Title;

        return new CampaignDefinition
        {
            CampaignId = campaignId,
            Title = title,
            ModuleRootPath = moduleRootPath,
            ManifestRelativePath = manifestRelativePath.Replace('\\', '/'),
            ScriptPath = scriptPath,
            Chapters = chapters,
        };
    }
}
