namespace TinyTBS.Game.Editor;

/// <summary>Editor workspace / writer failures (create, copy, open).</summary>
public sealed class EditorException : Exception
{
    public EditorException(string message)
        : base(message)
    {
    }

    public EditorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
