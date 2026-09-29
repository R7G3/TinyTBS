namespace TinyTBS.Engine.IO;

/// <summary>Stub when the host cannot open external URLs (tests / future locked-down builds).</summary>
public sealed class UnavailableExternalUriLauncher : IExternalUriLauncher
{
    public bool TryOpen(string url, out string? errorMessage)
    {
        errorMessage = "Opening links is not available on this platform.";
        return false;
    }
}
