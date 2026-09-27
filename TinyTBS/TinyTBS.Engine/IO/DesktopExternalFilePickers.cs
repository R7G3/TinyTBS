namespace TinyTBS.Engine.IO;

/// <summary>Factory for the default desktop <see cref="IExternalFilePicker"/> (NFD / Linux portal).</summary>
public static class DesktopExternalFilePickers
{
    /// <summary>
    /// Linux → <see cref="LinuxExternalFilePicker"/>; Windows/macOS → <see cref="NfdExternalFilePicker"/>;
    /// otherwise <see cref="UnavailableExternalFilePicker"/>.
    /// </summary>
    public static IExternalFilePicker CreateDefault()
    {
        if (OperatingSystem.IsLinux())
            return new LinuxExternalFilePicker();

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            return new NfdExternalFilePicker();

        return new UnavailableExternalFilePicker();
    }
}
