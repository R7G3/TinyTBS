using TinyTBS.Engine.IO;
using TinyTBS.Engine.Scripting;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Scripting.Models;

namespace TinyTBS.Game.Scripting;

/// <summary>Loads and invokes campaign meta scripts with a short timeout.</summary>
public sealed class CampaignScriptHost
{
    private readonly ICampaignScriptHooks _hooks;
    private readonly TimeSpan _hookTimeout;

    public CampaignScriptHost(ICampaignScriptHooks hooks, TimeSpan? hookTimeout = null)
    {
        _hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
        _hookTimeout = hookTimeout ?? TimeSpan.FromSeconds(2);
    }

    public static CampaignScriptHost Load(
        string? scriptPath,
        IFileContentProvider files,
        IScriptEngine scriptEngine,
        TimeSpan? hookTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(scriptEngine);

        ICampaignScriptHooks hooks;
        if (string.IsNullOrWhiteSpace(scriptPath) || !files.Exists(scriptPath))
        {
            hooks = new NoOpCampaignScriptHooks();
        }
        else
        {
            using var stream = files.OpenRead(scriptPath);
            using var reader = new StreamReader(stream);
            var sourceCode = reader.ReadToEnd();
            hooks = scriptEngine.LoadCampaignScript(sourceCode, Path.GetFileName(scriptPath));
        }

        return new CampaignScriptHost(hooks, hookTimeout);
    }

    public CampaignScriptMutation InvokeCampaignStarted(CampaignRunState run) =>
        Invoke("OnCampaignStarted", run, (context, hooks) => hooks.OnCampaignStarted(context));

    public CampaignScriptMutation InvokeChapterStarted(CampaignRunState run) =>
        Invoke("OnChapterStarted", run, (context, hooks) => hooks.OnChapterStarted(context));

    public CampaignScriptMutation InvokeChapterWon(CampaignRunState run) =>
        Invoke("OnChapterWon", run, (context, hooks) => hooks.OnChapterWon(context));

    public CampaignScriptMutation InvokeChapterLost(CampaignRunState run) =>
        Invoke("OnChapterLost", run, (context, hooks) => hooks.OnChapterLost(context));

    public CampaignScriptMutation InvokeCampaignCompleted(CampaignRunState run) =>
        Invoke("OnCampaignCompleted", run, (context, hooks) => hooks.OnCampaignCompleted(context));

    private CampaignScriptMutation Invoke(
        string hookName,
        CampaignRunState run,
        Action<CampaignScriptContext, ICampaignScriptHooks> call)
    {
        ArgumentNullException.ThrowIfNull(run);
        return ScriptHookInvoker.Invoke(hookName, _hookTimeout, () =>
        {
            var mutation = new CampaignScriptMutation();
            var context = new CampaignScriptContext(
                run.CampaignId,
                run.CurrentLevelId,
                run.Extensions,
                mutation);
            call(context, _hooks);
            return mutation;
        });
    }
}
