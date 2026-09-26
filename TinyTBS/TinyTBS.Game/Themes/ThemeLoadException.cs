namespace TinyTBS.Game.Themes;

/// <summary>Thrown when a theme module cannot be loaded.</summary>
public sealed class ThemeLoadException : Exception
{
    public ThemeLoadException(string message)
        : base(message)
    {
    }

    public ThemeLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
