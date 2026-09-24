namespace TinyTBS.Game.Buildings;

/// <summary>Thrown when a buildings module or building JSON cannot be loaded.</summary>
public sealed class BuildingLoadException : Exception
{
    public BuildingLoadException(string message)
        : base(message)
    {
    }

    public BuildingLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
