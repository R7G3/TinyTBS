namespace TinyTBS.Engine.IO;

/// <summary>
/// Linux file picker: prefer xdg-desktop-portal inside Flatpak/Snap (and when forced),
/// otherwise nativefiledialog-extended (NFD). Each side falls back to the other on failure.
/// </summary>
public sealed class LinuxExternalFilePicker : IExternalFilePicker
{
    private readonly IExternalFilePicker _portal;
    private readonly IExternalFilePicker _nfd;

    public LinuxExternalFilePicker()
        : this(new XdgDesktopPortalExternalFilePicker(), new NfdExternalFilePicker())
    {
    }

    public LinuxExternalFilePicker(IExternalFilePicker portal, IExternalFilePicker nfd)
    {
        _portal = portal ?? throw new ArgumentNullException(nameof(portal));
        _nfd = nfd ?? throw new ArgumentNullException(nameof(nfd));
    }

    public string? PickOpenFile(ExternalFilePickRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (LinuxSandbox.PreferXdgDesktopPortal())
            return TryPick(_portal, request) ?? TryPick(_nfd, request);

        return TryPick(_nfd, request) ?? TryPick(_portal, request);
    }

    private static string? TryPick(IExternalFilePicker picker, ExternalFilePickRequest request)
    {
        try
        {
            return picker.PickOpenFile(request);
        }
        catch
        {
            return null;
        }
    }
}
