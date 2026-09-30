using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Engine.Scripting;
using TinyTBS.Game.Campaigns;
using TinyTBS.Scripting.Api;

namespace TinyTBS.Game.Scripting;

/// <summary>
/// Loads and invokes campaign meta scripts. Each hook writes into a mutation object; the host applies
/// it only when the hook finishes within budget. A failing hook disables the script for the rest of
/// the campaign run instead of breaking progress.
/// </summary>
public sealed class CampaignScriptHost : IDisposable
{
    private readonly LoadedScript<ICampaignScriptHooks> _script;
    private readonly TimeSpan _hookTimeout;

    public CampaignScriptHost(LoadedScript<ICampaignScriptHooks> script, TimeSpan? hookTimeout = null)
    {
        _script = script ?? throw new ArgumentNullException(nameof(script));
        _hookTimeout = hookTimeout ?? ScriptExecution.DefaultTimeout;
    }

    /// <summary>Why the campaign script was switched off; null while it runs normally.</summary>
    public string? FailureMessage { get; set; }

    public static CampaignScriptHost Disabled(string failureMessage) =>
        new(new LoadedScript<ICampaignScriptHooks>(new NoOpCampaignScriptHooks()))
        {
            FailureMessage = failureMessage,
        };

    public static CampaignScriptHost Load(
        string? scriptPath,
        IFileContentProvider files,
        IScriptEngine scriptEngine,
        TimeSpan? hookTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(scriptEngine);

        if (string.IsNullOrWhiteSpace(scriptPath) || !files.Exists(scriptPath))
        {
            return new CampaignScriptHost(
                new LoadedScript<ICampaignScriptHooks>(new NoOpCampaignScriptHooks()),
                hookTimeout);
        }

        using var stream = files.OpenRead(scriptPath);
        using var reader = new StreamReader(stream);
        var sourceCode = reader.ReadToEnd();
        return new CampaignScriptHost(
            scriptEngine.LoadCampaignScript(sourceCode, Path.GetFileName(scriptPath)),
            hookTimeout);
    }

    public CampaignScriptMutation InvokeCampaignStarted(CampaignRunState run) =>
        Invoke("OnCampaignStarted", run, static (context, hooks) => hooks.OnCampaignStarted(context));

    public CampaignScriptMutation InvokeChapterStarted(CampaignRunState run) =>
        Invoke("OnChapterStarted", run, static (context, hooks) => hooks.OnChapterStarted(context));

    public CampaignScriptMutation InvokeChapterWon(CampaignRunState run) =>
        Invoke("OnChapterWon", run, static (context, hooks) => hooks.OnChapterWon(context));

    public CampaignScriptMutation InvokeChapterLost(CampaignRunState run) =>
        Invoke("OnChapterLost", run, static (context, hooks) => hooks.OnChapterLost(context));

    public CampaignScriptMutation InvokeCampaignCompleted(CampaignRunState run) =>
        Invoke("OnCampaignCompleted", run, static (context, hooks) => hooks.OnCampaignCompleted(context));

    public void Dispose() => _script.Dispose();

    private CampaignScriptMutation Invoke(
        string hookName,
        CampaignRunState run,
        Action<CampaignScriptContext, ICampaignScriptHooks> call)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (FailureMessage is not null)
            return new CampaignScriptMutation();

        var mutation = new CampaignScriptMutation();
        var context = new CampaignScriptContext(run.CampaignId, run.CurrentLevelId, run.Extensions, mutation);
        var hooks = _script.Hooks;
        try
        {
            ScriptExecution.Run(hookName, _hookTimeout, () => call(context, hooks));
        }
        catch (ScriptHostException exception)
        {
            FailureMessage = exception.Message;
            GameLog.Error($"Campaign script disabled after '{hookName}' failed.", exception);
            return new CampaignScriptMutation();
        }

        return mutation;
    }
}
