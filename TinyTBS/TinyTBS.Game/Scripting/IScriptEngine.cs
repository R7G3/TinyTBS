namespace TinyTBS.Game.Scripting;

/// <summary>Loads and compiles map board scripts.</summary>
public interface IScriptEngine
{
    /// <summary>
    /// Compiles <paramref name="sourceCode"/> into hook implementations.
    /// Empty / comment-only source yields a no-op script.
    /// </summary>
    IMapScriptHooks LoadMapScript(string sourceCode, string? sourceFileName = null);
}
