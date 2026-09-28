namespace TinyTBS.Game.Saves;

/// <summary>Thrown when a match save cannot be read, written, or applied.</summary>
public sealed class MatchSaveException : Exception
{
    public MatchSaveException(string message)
        : base(message)
    {
    }

    public MatchSaveException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
