namespace TinyTBS.Game.Modules;

/// <summary>Thrown when packing a module into <c>.tinymod.zip</c> fails.</summary>
public sealed class TinymodExportException : Exception
{
    public TinymodExportException(string message)
        : base(message)
    {
    }

    public TinymodExportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
