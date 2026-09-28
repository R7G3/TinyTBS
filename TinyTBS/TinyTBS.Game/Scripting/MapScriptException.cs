using TinyTBS.Engine.Scripting;

namespace TinyTBS.Game.Scripting;

/// <summary>Game-facing alias for <see cref="ScriptHostException"/> (map / campaign scripts).</summary>
public sealed class MapScriptException : ScriptHostException
{
    public MapScriptException(string message)
        : base(message)
    {
    }

    public MapScriptException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
