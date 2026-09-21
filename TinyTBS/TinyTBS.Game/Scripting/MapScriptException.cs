namespace TinyTBS.Game.Scripting;

public sealed class MapScriptException : Exception
{
    public MapScriptException(string message) : base(message)
    {
    }

    public MapScriptException(string message, Exception inner) : base(message, inner)
    {
    }
}
