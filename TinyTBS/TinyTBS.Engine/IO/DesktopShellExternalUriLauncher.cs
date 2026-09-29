using System.Diagnostics;

namespace TinyTBS.Engine.IO;

/// <summary>
/// Desktop: hand http(s) URLs to the OS shell (<c>UseShellExecute</c> → default browser).
/// </summary>
public sealed class DesktopShellExternalUriLauncher : IExternalUriLauncher
{
    public bool TryOpen(string url, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(url)
            || (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "Invalid URL.";
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }
}
