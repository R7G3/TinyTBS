namespace TinyTBS.Game.Modules;

/// <summary>Thrown when installing or removing a <c>.tinymod.zip</c> fails.</summary>
public sealed class TinymodInstallException : Exception
{
    public TinymodInstallException(string message)
        : base(message)
    {
    }

    public TinymodInstallException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
