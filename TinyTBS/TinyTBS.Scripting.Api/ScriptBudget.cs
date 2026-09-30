using System.ComponentModel;

namespace TinyTBS.Scripting.Api;

/// <summary>
/// Execution budget for script code. The script compiler inserts <see cref="Tick"/> into loops and jumps
/// and <see cref="EnterFrame"/> into every function body; scripts cannot call these members themselves.
/// Budgets are per hook call and deterministic (counted steps, not wall-clock time).
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ScriptBudget
{
    [ThreadStatic]
    private static ScriptBudgetState? _active;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Tick() => RequireActive().Tick();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ScriptBudgetFrame EnterFrame()
    {
        var state = RequireActive();
        state.EnterFrame();
        return new ScriptBudgetFrame(state);
    }

    internal static void Run(ScriptBudgetState state, Action action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        var previous = _active;
        _active = state;
        try
        {
            action();
        }
        finally
        {
            _active = previous;
        }
    }

    private static ScriptBudgetState RequireActive() =>
        _active ?? throw new ScriptBudgetExceededException("Script code ran outside a host hook.");
}
