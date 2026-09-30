using TinyTBS.Engine.Scripting;
using TinyTBS.Scripting.Api;

namespace TinyTBS.Game.Scripting;

/// <summary>Runs script code under a per-call step/depth budget and a wall-clock timeout.</summary>
internal static class ScriptExecution
{
    /// <summary>Loop iterations, jumps and calls allowed per hook; counted, so identical on every machine.</summary>
    public const long StepLimit = 10_000_000;

    public const int CallDepthLimit = 200;

    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    public static void Run(string hookName, TimeSpan timeout, Action action)
    {
        var budget = new ScriptBudgetState(StepLimit, CallDepthLimit);
        ScriptHookInvoker.Invoke(
            hookName,
            timeout,
            () => ScriptBudget.Run(budget, action),
            onTimeout: budget.Cancel);
    }

    public static TResult Run<TResult>(string hookName, TimeSpan timeout, Func<TResult> action)
    {
        TResult? result = default;
        Run(hookName, timeout, () => { result = action(); });
        return result!;
    }
}
