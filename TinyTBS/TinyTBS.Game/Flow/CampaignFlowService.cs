using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Rules.Ai;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Match.Session;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Flow;

/// <summary>
/// Campaign progress around match boundaries: start or resume a run, fire chapter hooks when a match opens,
/// and build the request for the next chapter. The HUD only displays the result.
/// </summary>
public sealed class CampaignFlowService
{
    private readonly CampaignProgressService _progress;

    public CampaignFlowService(IUserDataPaths userDataPaths, IFileSystem files)
    {
        _progress = new CampaignProgressService(userDataPaths, files);
    }

    public CampaignRunState BeginOrResumeRun(
        CampaignDefinition campaign,
        string scenarioModuleId,
        string levelId,
        MatchContentComposition composition,
        IReadOnlyList<MatchPlayerSeat> playerSeats,
        int unitCap) =>
        _progress.BeginOrResumeRun(campaign, scenarioModuleId, levelId, composition, playerSeats, unitCap);

    /// <summary>
    /// Copies composition, seats and unit cap from the loaded match onto the run, attaches the run,
    /// then fires chapter-start hooks. The loading screen does not edit the run itself.
    /// </summary>
    public void AttachOpenedMatch(CampaignRunState run, MatchRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(runtime);
        run.Composition ??= runtime.Composition;
        run.PlayerSeats ??= runtime.PlayerSeats;
        run.UnitCap ??= runtime.State.UnitCap;
        runtime.CampaignRun = run;
        try
        {
            NotifyMatchOpened(run);
        }
        finally
        {
            // Subscribe even when chapter-start hooks fail, and pick up a match that already ended
            // during load (the event replays for a late subscriber).
            runtime.MatchEnded -= RecordFinishedChapter;
            runtime.MatchEnded += RecordFinishedChapter;
        }
    }

    /// <summary>Persists the chapter once <see cref="MatchRuntime.MatchEnded"/> fires. The HUD only reads the result.</summary>
    private void RecordFinishedChapter(MatchRuntime runtime)
    {
        if (runtime.CampaignChapterResult is not null || runtime.CampaignRun is not { } run)
            return;

        var campaign = _progress.TryLoadDefinition(run.ScenarioModuleId);
        if (campaign is null)
        {
            runtime.SetCampaignChapterResult(new MatchCampaignResult(ShowNextChapter: false, ShowRetry: true, Note: null));
            return;
        }

        var localWon = IsLocalPlayerWinner(runtime);
        try
        {
            var chapterEnd = localWon
                ? _progress.ApplyChapterWon(run, campaign)
                : _progress.ApplyChapterLost(run, campaign);
            var note = !localWon
                ? null
                : chapterEnd.HasNextChapter
                    ? $"Next: {chapterEnd.NextLevelId}"
                    : "Campaign complete";
            runtime.SetCampaignChapterResult(new MatchCampaignResult(chapterEnd.HasNextChapter, ShowRetry: !localWon, note));
        }
        catch (Exception exception)
        {
            GameLog.Error("Recording the campaign chapter result failed.", exception);
            runtime.SetCampaignChapterResult(new MatchCampaignResult(
                ShowNextChapter: false,
                ShowRetry: true,
                Note: $"Progress error: {exception.Message}"));
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

    /// <summary>Best-effort chapter-start hook once the match session exists. A missing definition still persists the run.</summary>
    public void NotifyMatchOpened(CampaignRunState run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var campaign = _progress.TryLoadDefinition(run.ScenarioModuleId);
        if (campaign is not null)
            _progress.NotifyChapterStarted(run, campaign);
        else
            _progress.Persist(run);
    }

    public HashSet<string> UnlockedChapterIds(string scenarioModuleId, CampaignDefinition campaign)
    {
        var progress = _progress.Store.TryLoadLatestForCampaign(scenarioModuleId, campaign.CampaignId);
        return CampaignProgressFactory.BuildUnlockedSet(progress, campaign);
    }

    public string PreferPlayableLevelId(string scenarioModuleId, CampaignDefinition campaign)
    {
        var progress = _progress.Store.TryLoadLatestForCampaign(scenarioModuleId, campaign.CampaignId);
        return CampaignProgressFactory.PreferPlayableLevelId(campaign, progress);
    }

    /// <summary>
    /// Next chapter and Retry both start <see cref="CampaignRunState.CurrentLevelId"/>:
    /// a won chapter has already advanced it, a lost one has not.
    /// </summary>
    public MatchStartRequest CreateChapterRequest(CampaignRunState run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return new MatchStartRequest
        {
            ScenarioModuleId = run.ScenarioModuleId,
            LevelId = run.CurrentLevelId,
            Composition = run.Composition,
            PlayerCount = run.PlayerSeats?.Count,
            UnitCap = run.UnitCap,
            PlayerSeats = run.PlayerSeats,
            CampaignRun = run,
        };
    }

    public void LogMatchOpenFailure(Exception exception) =>
        GameLog.Warning("Campaign chapter start hooks failed.", exception);
}
