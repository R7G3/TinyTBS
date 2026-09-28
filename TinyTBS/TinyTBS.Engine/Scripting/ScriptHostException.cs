namespace TinyTBS.Engine.Scripting;

/// <summary>Thrown when script validation, compilation, or hook invocation fails.</summary>
public class ScriptHostException : Exception
{
    public ScriptHostException(string message)
        : base(message)
    {
    }

    public ScriptHostException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
