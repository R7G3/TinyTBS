using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Ai;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Saves;
using TinyTBS.Game.Scripting;
using TinyTBS.Scripting.Api;

namespace TinyTBS.Game.Campaigns;

/// <summary>Persists progress and runs campaign script hooks around chapter transitions.</summary>
public sealed class CampaignProgressService
{
    private readonly IUserDataPaths _userDataPaths;
    private readonly IFileContentProvider _files;
    private readonly IScriptEngine _scriptEngine;
    private readonly CampaignProgressStore _store;

    public CampaignProgressService(
        IUserDataPaths userDataPaths,
        IFileContentProvider files,
        IScriptEngine? scriptEngine = null)
    {
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _scriptEngine = scriptEngine ?? new RoslynMapScriptEngine();
        _store = new CampaignProgressStore(userDataPaths);
    }

    public CampaignProgressStore Store => _store;

    public CampaignDefinition? TryLoadDefinition(string scenarioModuleId)
    {
        try
        {
            var locator = new ContentModuleLocator(_files, _userDataPaths);
            var root = locator.ResolveModuleRoot(scenarioModuleId);
            var scenario = ScenarioModuleLoader.Load(root, _files);
            return CampaignLoader.TryLoadFromScenario(root, scenario.CampaignManifestRelativePath, _files);
        }
        catch (Exception exception)
        {
            GameLog.Warning($"Campaign definition for '{scenarioModuleId}' could not be loaded.", exception);
            return null;
        }
    }

    public string Persist(CampaignRunState run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var contentSetup = run.Composition is null
            ? null
            : CampaignProgressFactory.ToContentSetup(run.Composition, moduleVersions: null);
        var document = run.ToProgressDocument(contentSetup);
        var path = _store.Write(document);
        run.ProgressFilePath = path;
        return path;
    }

    public CampaignScriptHost LoadScriptHost(CampaignDefinition campaign) =>
        CampaignScriptHost.Load(campaign.ScriptPath, _files, _scriptEngine);

    /// <summary>
    /// Starts a new run of <paramref name="campaign"/> or resumes the latest one at <paramref name="levelId"/>,
    /// and writes the progress file. A new run also fires <c>OnCampaignStarted</c> (best-effort).
    /// </summary>
    public CampaignRunState BeginOrResumeRun(
        CampaignDefinition campaign,
        string scenarioModuleId,
        string levelId,
        MatchContentComposition composition,
        IReadOnlyList<MatchPlayerSeat> playerSeats,
        int unitCap)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleId);
        ArgumentNullException.ThrowIfNull(playerSeats);

        var existing = _store.TryLoadLatestForCampaign(scenarioModuleId, campaign.CampaignId);
        var savedSeats = playerSeats.Select(MatchSaveSeatCodec.ToSave).ToList();
        var progress = existing is null
            ? CampaignProgressFactory.CreateNew(campaign, scenarioModuleId, composition, savedSeats, unitCap)
            : CampaignProgressFactory.AtChapter(existing, campaign, levelId, composition, savedSeats, unitCap);

        var path = _store.Write(progress);
        var run = CampaignRunState.FromProgress(progress, composition, playerSeats.ToArray());
        run.ProgressFilePath = path;

        if (existing is null)
        {
            try
            {
                NotifyCampaignStarted(run, campaign);
            }
            catch (Exception exception)
            {
                // Campaign start hooks are best-effort; the chapter still starts.
                GameLog.Warning("Campaign start hooks failed.", exception);
            }
        }

        return run;
    }

    public void NotifyChapterStarted(CampaignRunState run, CampaignDefinition campaign)
    {
        using var host = OpenScriptHost(run, campaign);
        var mutation = host.InvokeChapterStarted(run);
        RememberScriptFailure(run, host);
        ApplySoftMutation(run, mutation);
        Persist(run);
    }

    public void NotifyCampaignStarted(CampaignRunState run, CampaignDefinition campaign)
    {
        using var host = OpenScriptHost(run, campaign);
        var mutation = host.InvokeCampaignStarted(run);
        RememberScriptFailure(run, host);
        ApplySoftMutation(run, mutation);
        Persist(run);
    }

    public ChapterEndResult ApplyChapterWon(CampaignRunState run, CampaignDefinition campaign)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(campaign);

        using var host = OpenScriptHost(run, campaign);
        var mutation = host.InvokeChapterWon(run);
        RememberScriptFailure(run, host);
        var nextLevelId = CampaignChapterAdvancer.ApplyChapterWon(run, campaign, mutation);
        if (nextLevelId is null)
        {
            var completedMutation = host.InvokeCampaignCompleted(run);
            RememberScriptFailure(run, host);
            ApplySoftMutation(run, completedMutation);
        }

        Persist(run);
        return new ChapterEndResult
        {
            PlayerWon = true,
            HasNextChapter = nextLevelId is not null,
            NextLevelId = nextLevelId,
        };
    }

    public ChapterEndResult ApplyChapterLost(CampaignRunState run, CampaignDefinition campaign)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(campaign);

        using var host = OpenScriptHost(run, campaign);
        var mutation = host.InvokeChapterLost(run);
        RememberScriptFailure(run, host);
        CampaignChapterAdvancer.ApplyChapterLost(run, campaign, mutation);
        Persist(run);
        return new ChapterEndResult
        {
            PlayerWon = false,
            HasNextChapter = false,
            NextLevelId = run.CurrentLevelId,
        };
    }

    private CampaignScriptHost OpenScriptHost(CampaignRunState run, CampaignDefinition campaign) =>
        run.ScriptFailureMessage is { } existingFailure
            ? CampaignScriptHost.Disabled(existingFailure)
            : LoadScriptHost(campaign);

    private static void RememberScriptFailure(CampaignRunState run, CampaignScriptHost host)
    {
        if (host.FailureMessage is { } failure)
            run.ScriptFailureMessage = failure;
    }

    private static void ApplySoftMutation(CampaignRunState run, CampaignScriptMutation mutation)
    {
        foreach (var pair in mutation.Flags)
            run.Extensions[pair.Key] = pair.Value;

        if (!string.IsNullOrWhiteSpace(mutation.ForcedNextLevelId))
            run.PendingNextLevelId = mutation.ForcedNextLevelId.Trim();
    }
}
