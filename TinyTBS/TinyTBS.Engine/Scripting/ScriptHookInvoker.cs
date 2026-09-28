namespace TinyTBS.Engine.Scripting;

/// <summary>Runs a script hook on a background task with a hard timeout.</summary>
public static class ScriptHookInvoker
{
    public static void Invoke(string hookName, TimeSpan timeout, Action action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hookName);
        ArgumentNullException.ThrowIfNull(action);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Hook timeout must be positive.");

        using var cancellation = new CancellationTokenSource(timeout);
        var task = Task.Run(action, cancellation.Token);
        try
        {
            if (!task.Wait(timeout))
            {
                throw new ScriptHostException(
                    $"Script hook '{hookName}' timed out after {timeout.TotalSeconds:0.#}s.");
            }
        }
        catch (AggregateException aggregateException)
        {
            var inner = aggregateException.InnerException ?? aggregateException;
            if (inner is ScriptHostException)
                throw inner;

            throw new ScriptHostException(
                $"Script hook '{hookName}' failed: {inner.Message}",
                inner);
        }
    }

    public static TResult Invoke<TResult>(string hookName, TimeSpan timeout, Func<TResult> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        TResult? result = default;
        Invoke(hookName, timeout, () => { result = action(); });
        return result!;
    }
}
