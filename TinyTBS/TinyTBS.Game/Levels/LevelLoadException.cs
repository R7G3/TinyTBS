namespace TinyTBS.Game.Levels;

public sealed class LevelLoadException : Exception
{
    public LevelLoadException(string message) : base(message)
    {
    }

    public LevelLoadException(string message, Exception inner) : base(message, inner)
    {
    }
}
