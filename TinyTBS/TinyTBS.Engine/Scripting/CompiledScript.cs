using System.Runtime.Loader;

namespace TinyTBS.Engine.Scripting;

/// <summary>
/// A compiled script loaded into its own collectible load context.
/// Dispose once the script instance is no longer used so the assembly can be unloaded.
/// </summary>
public sealed class CompiledScript : IDisposable
{
    private AssemblyLoadContext? _loadContext;

    internal CompiledScript(AssemblyLoadContext loadContext, Type scriptType)
    {
        _loadContext = loadContext;
        ScriptType = scriptType;
    }

    /// <summary>The generated class that implements the hook interface.</summary>
    public Type ScriptType { get; }

    public void Dispose()
    {
        var loadContext = _loadContext;
        _loadContext = null;
        loadContext?.Unload();
    }
}
