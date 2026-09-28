using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Scripting;
using TinyTBS.Game.Scripting.Models;

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
        catch (Exception)
        {
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

    public void NotifyChapterStarted(CampaignRunState run, CampaignDefinition campaign)
    {
        var host = LoadScriptHost(campaign);
        var mutation = host.InvokeChapterStarted(run);
        ApplySoftMutation(run, mutation);
        Persist(run);
    }

    public void NotifyCampaignStarted(CampaignRunState run, CampaignDefinition campaign)
    {
        var host = LoadScriptHost(campaign);
        var mutation = host.InvokeCampaignStarted(run);
        ApplySoftMutation(run, mutation);
        Persist(run);
    }

    public ChapterEndResult ApplyChapterWon(CampaignRunState run, CampaignDefinition campaign)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(campaign);

        var host = LoadScriptHost(campaign);
        var mutation = host.InvokeChapterWon(run);
        var nextLevelId = CampaignChapterAdvancer.ApplyChapterWon(run, campaign, mutation);
        if (nextLevelId is null)
        {
            var completedMutation = host.InvokeCampaignCompleted(run);
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

        var host = LoadScriptHost(campaign);
        var mutation = host.InvokeChapterLost(run);
        CampaignChapterAdvancer.ApplyChapterLost(run, campaign, mutation);
        Persist(run);
        return new ChapterEndResult
        {
            PlayerWon = false,
            HasNextChapter = false,
            NextLevelId = run.CurrentLevelId,
        };
    }

    private static void ApplySoftMutation(CampaignRunState run, CampaignScriptMutation mutation)
    {
        foreach (var pair in mutation.Flags)
            run.Extensions[pair.Key] = pair.Value;

        if (!string.IsNullOrWhiteSpace(mutation.ForcedNextLevelId))
            run.PendingNextLevelId = mutation.ForcedNextLevelId.Trim();
    }
}
