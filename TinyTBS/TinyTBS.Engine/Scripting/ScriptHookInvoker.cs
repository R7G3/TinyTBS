using System.Reflection;

namespace TinyTBS.Engine.Scripting;

/// <summary>Runs a script hook on a background task with a hard timeout.</summary>
public static class ScriptHookInvoker
{
    /// <param name="onTimeout">
    /// Called when the hook overruns. .NET cannot abort the worker thread, so the caller should make the
    /// script stop itself (for example by cancelling its execution budget).
    /// </param>
    public static void Invoke(string hookName, TimeSpan timeout, Action action, Action? onTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hookName);
        ArgumentNullException.ThrowIfNull(action);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Hook timeout must be positive.");

        var task = Task.Run(action);
        bool completed;
        try
        {
            completed = task.Wait(timeout);
        }
        catch (AggregateException aggregateException)
        {
            var inner = aggregateException.InnerException ?? aggregateException;
            if (inner is TargetInvocationException { InnerException: { } invocationInner })
                inner = invocationInner;
            if (inner is ScriptHostException)
                throw inner;

            throw new ScriptHostException($"Script hook '{hookName}' failed: {inner.Message}", inner);
        }

        if (completed)
            return;

        onTimeout?.Invoke();
        task.ContinueWith(
            static finished => _ = finished.Exception,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        throw new ScriptHostException(
            $"Script hook '{hookName}' timed out after {timeout.TotalSeconds:0.#}s.");
    }

    public static TResult Invoke<TResult>(
        string hookName,
        TimeSpan timeout,
        Func<TResult> action,
        Action? onTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        TResult? result = default;
        Invoke(hookName, timeout, () => { result = action(); }, onTimeout);
        return result!;
    }
}
