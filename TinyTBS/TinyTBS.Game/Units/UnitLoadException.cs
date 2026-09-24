namespace TinyTBS.Game.Units;

/// <summary>Thrown when a units module or unit JSON cannot be loaded.</summary>
public sealed class UnitLoadException : Exception
{
    public UnitLoadException(string message)
        : base(message)
    {
    }

    public UnitLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
