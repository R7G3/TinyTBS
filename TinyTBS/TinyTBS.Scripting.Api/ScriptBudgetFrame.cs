using System.ComponentModel;

namespace TinyTBS.Scripting.Api;

/// <summary>Call-depth scope returned by <see cref="ScriptBudget.EnterFrame"/>.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct ScriptBudgetFrame : IDisposable
{
    private readonly ScriptBudgetState? _state;

    internal ScriptBudgetFrame(ScriptBudgetState state)
    {
        _state = state;
    }

    public void Dispose() => _state?.ExitFrame();
}
