namespace TinyTBS.Game.Maps;

public sealed class MapLoadException : Exception
{
    public MapLoadException(string message) : base(message)
    {
    }

    public MapLoadException(string message, Exception inner) : base(message, inner)
    {
    }
}
