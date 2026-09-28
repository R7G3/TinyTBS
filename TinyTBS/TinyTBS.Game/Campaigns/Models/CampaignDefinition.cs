namespace TinyTBS.Game.Campaigns.Models;

/// <summary>Loaded <c>Campaign/campaign.json</c>.</summary>
public sealed class CampaignDefinition
{
    public required string CampaignId { get; init; }

    public required string Title { get; init; }

    public required string ModuleRootPath { get; init; }

    /// <summary>Relative path of the json under the scenario module.</summary>
    public required string ManifestRelativePath { get; init; }

    /// <summary>Optional <c>Campaign/script.cs</c> absolute path.</summary>
    public string? ScriptPath { get; init; }

    public required IReadOnlyList<CampaignChapterDefinition> Chapters { get; init; }

    public int IndexOfLevel(string levelId)
    {
        for (var i = 0; i < Chapters.Count; i++)
        {
            if (string.Equals(Chapters[i].LevelId, levelId, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    public string? NextLevelIdAfter(string levelId)
    {
        var index = IndexOfLevel(levelId);
        if (index < 0 || index + 1 >= Chapters.Count)
            return null;
        return Chapters[index + 1].LevelId;
    }
}
