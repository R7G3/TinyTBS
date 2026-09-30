using TinyTBS.Engine.Scripting;

namespace TinyTBS.Game.Scripting;

/// <summary>Script hooks plus the load context that must be unloaded when the hooks are no longer used.</summary>
public sealed class LoadedScript<THooks> : IDisposable
    where THooks : class
{
    private CompiledScript? _compiled;

    public LoadedScript(THooks hooks, CompiledScript? compiled = null)
    {
        Hooks = hooks ?? throw new ArgumentNullException(nameof(hooks));
        _compiled = compiled;
    }

    public THooks Hooks { get; }

    public void Dispose()
    {
        var compiled = _compiled;
        _compiled = null;
        compiled?.Dispose();
    }
}
