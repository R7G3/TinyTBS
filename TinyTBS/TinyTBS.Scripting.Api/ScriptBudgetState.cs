namespace TinyTBS.Scripting.Api;

/// <summary>Remaining steps and call depth for one hook call; cancellable by the host after its timeout.</summary>
internal sealed class ScriptBudgetState
{
    private readonly int _maxDepth;
    private long _remainingSteps;
    private int _depth;
    private volatile bool _cancelled;

    public ScriptBudgetState(long maxSteps, int maxDepth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSteps);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDepth);
        _remainingSteps = maxSteps;
        _maxDepth = maxDepth;
    }

    /// <summary>Makes the next step throw, so a hook that outlived its timeout stops at its next loop or call.</summary>
    public void Cancel() => _cancelled = true;

    public void Tick()
    {
        if (_cancelled)
            throw new ScriptBudgetExceededException("Script hook was stopped after its time limit.");
        if (--_remainingSteps < 0)
            throw new ScriptBudgetExceededException("Script exceeded its step budget (endless loop?).");
    }

    public void EnterFrame()
    {
        Tick();
        if (_depth >= _maxDepth)
            throw new ScriptBudgetExceededException($"Script exceeded the maximum call depth of {_maxDepth}.");
        _depth++;
    }

    public void ExitFrame()
    {
        if (_depth > 0)
            _depth--;
    }
}
