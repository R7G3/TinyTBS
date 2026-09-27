namespace TinyTBS.Engine.IO;

/// <summary>Detects Flatpak/Snap via filesystem markers (no environment variables).</summary>
internal static class LinuxSandbox
{
    /// <summary>Prefer xdg-desktop-portal inside Flatpak or Snap.</summary>
    public static bool PreferXdgDesktopPortal() => IsFlatpak() || IsSnap();

    private static bool IsFlatpak()
    {
        // Canonical Flatpak marker inside the sandbox root.
        try
        {
            return File.Exists("/.flatpak-info");
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSnap()
    {
        // Snap apps run from /snap/<name>/... (or /var/lib/snapd/snap/...).
        try
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath) && IsUnderSnapRoot(processPath))
                return true;

            var baseDirectory = AppContext.BaseDirectory;
            return !string.IsNullOrWhiteSpace(baseDirectory) && IsUnderSnapRoot(baseDirectory);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUnderSnapRoot(string path)
    {
        var full = Path.GetFullPath(path)
            .Replace('\\', '/')
            .TrimEnd('/');

        return full.StartsWith("/snap/", StringComparison.Ordinal)
            || full.Equals("/snap", StringComparison.Ordinal)
            || full.StartsWith("/var/lib/snapd/snap/", StringComparison.Ordinal);
    }
}
