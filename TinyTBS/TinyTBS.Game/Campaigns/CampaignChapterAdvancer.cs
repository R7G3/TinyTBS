using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Scripting.Models;

namespace TinyTBS.Game.Campaigns;

/// <summary>Applies chapter win/loss and script routing to progress.</summary>
public static class CampaignChapterAdvancer
{
    /// <summary>
    /// Applies win routing. Returns the next chapter level id, or null when the campaign is complete.
    /// </summary>
    public static string? ApplyChapterWon(
        CampaignRunState run,
        CampaignDefinition campaign,
        CampaignScriptMutation mutation)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(mutation);

        ApplyMutation(run, campaign, mutation);

        var nextLevelId = ResolveNextLevelId(run, campaign, mutation);
        if (nextLevelId is null)
        {
            run.PendingNextLevelId = null;
            return null;
        }

        Unlock(run, nextLevelId);
        run.CurrentLevelId = nextLevelId;
        run.PendingNextLevelId = null;
        return nextLevelId;
    }

    public static void ApplyChapterLost(
        CampaignRunState run,
        CampaignDefinition campaign,
        CampaignScriptMutation mutation)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(mutation);

        ApplyMutation(run, campaign, mutation);
        run.PendingNextLevelId = null;
    }

    private static string? ResolveNextLevelId(
        CampaignRunState run,
        CampaignDefinition campaign,
        CampaignScriptMutation mutation)
    {
        if (!string.IsNullOrWhiteSpace(mutation.ForcedNextLevelId))
            return mutation.ForcedNextLevelId.Trim();

        if (!string.IsNullOrWhiteSpace(run.PendingNextLevelId))
            return run.PendingNextLevelId;

        return campaign.NextLevelIdAfter(run.CurrentLevelId);
    }

    private static void ApplyMutation(
        CampaignRunState run,
        CampaignDefinition campaign,
        CampaignScriptMutation mutation)
    {
        foreach (var pair in mutation.Flags)
            run.Extensions[pair.Key] = pair.Value;

        if (!string.IsNullOrWhiteSpace(mutation.ReplacedLevelId)
            && !string.IsNullOrWhiteSpace(mutation.ReplaceWithLevelId))
        {
            var from = mutation.ReplacedLevelId.Trim();
            var to = mutation.ReplaceWithLevelId.Trim();
            if (campaign.IndexOfLevel(to) < 0)
            {
                throw new MatchContentCompositionException(
                    $"ReplaceLevel target '{to}' is not in campaign '{campaign.CampaignId}'.");
            }

            if (string.Equals(run.CurrentLevelId, from, StringComparison.Ordinal))
                run.CurrentLevelId = to;

            for (var i = 0; i < run.UnlockedLevelIds.Count; i++)
            {
                if (string.Equals(run.UnlockedLevelIds[i], from, StringComparison.Ordinal))
                    run.UnlockedLevelIds[i] = to;
            }

            Unlock(run, to);
        }

        if (!string.IsNullOrWhiteSpace(mutation.SkippedLevelId))
        {
            var skipped = mutation.SkippedLevelId.Trim();
            var afterSkip = campaign.NextLevelIdAfter(skipped);
            if (afterSkip is not null)
            {
                Unlock(run, afterSkip);
                // Treat like SetNextChapter(afterSkip) unless ForcedNext already set.
                if (string.IsNullOrWhiteSpace(mutation.ForcedNextLevelId))
                    run.PendingNextLevelId = afterSkip;
            }
        }

        if (!string.IsNullOrWhiteSpace(mutation.ForcedNextLevelId))
        {
            var forced = mutation.ForcedNextLevelId.Trim();
            if (campaign.IndexOfLevel(forced) < 0)
            {
                throw new MatchContentCompositionException(
                    $"SetNextChapter target '{forced}' is not in campaign '{campaign.CampaignId}'.");
            }

            Unlock(run, forced);
            run.PendingNextLevelId = forced;
        }
    }

    private static void Unlock(CampaignRunState run, string levelId)
    {
        if (!run.UnlockedLevelIds.Contains(levelId, StringComparer.Ordinal))
            run.UnlockedLevelIds.Add(levelId);
    }
}
