using TinyTBS.Engine.Diagnostics;
using TinyTBS.Game.Ai;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Match.Session;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>
/// Records a finished campaign chapter exactly once (progress file + campaign script hooks)
/// and describes the outcome for the result overlay.
/// </summary>
public sealed class MatchCampaignResultResolver
{
    private readonly CampaignProgressService _campaignService;

    public MatchCampaignResultResolver(CampaignProgressService campaignService)
    {
        _campaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
    }

    public MatchCampaignResult? Result { get; private set; }

    /// <summary>Call every frame; works only once, when a campaign match has ended.</summary>
    public void Update(MatchRuntime runtime)
    {
        if (Result is not null || runtime.CampaignRun is not { } run || !runtime.State.IsMatchOver)
            return;

        var campaign = _campaignService.TryLoadDefinition(run.ScenarioModuleId);
        if (campaign is null)
        {
            Result = new MatchCampaignResult(ShowNextChapter: false, ShowRetry: true, Note: null);
            return;
        }

        var localWon = IsLocalPlayerWinner(runtime);
        try
        {
            var chapterEnd = localWon
                ? _campaignService.ApplyChapterWon(run, campaign)
                : _campaignService.ApplyChapterLost(run, campaign);
            var note = !localWon
                ? null
                : chapterEnd.HasNextChapter
                    ? $"Next: {chapterEnd.NextLevelId}"
                    : "Campaign complete";
            Result = new MatchCampaignResult(chapterEnd.HasNextChapter, ShowRetry: !localWon, note);
        }
        catch (Exception exception)
        {
            GameLog.Error("Recording the campaign chapter result failed.", exception);
            Result = new MatchCampaignResult(
                ShowNextChapter: false,
                ShowRetry: true,
                Note: $"Progress error: {exception.Message}");
        }
    }

    private static bool IsLocalPlayerWinner(MatchRuntime runtime)
    {
        if (runtime.State.WinnerPlayerIndex is not int winner)
            return false;

        if (winner < 0 || winner >= runtime.PlayerSeats.Count)
            return winner == 0;

        return runtime.PlayerSeats[winner].Kind == MatchPlayerKind.Local;
    }
}
