namespace TinyTBS.Engine.IO;

/// <summary>
/// Opens http(s) URLs in the platform default browser (desktop shell now; Android Intent later).
/// Game code must not call <c>Process.Start</c> directly.
/// </summary>
public interface IExternalUriLauncher
{
    /// <summary>
    /// Opens <paramref name="url"/> externally. Returns <see langword="false"/> on failure
    /// (invalid scheme, unsupported platform, or OS error); <paramref name="errorMessage"/> may explain why.
    /// </summary>
    bool TryOpen(string url, out string? errorMessage);
}
