namespace TinyTBS.Core.IO;

/// <summary>
/// Desktop paths: InstallRoot = AppContext.BaseDirectory;
/// UserDataRoot = LocalApplicationData/TinyTBS.
/// </summary>
public sealed class DesktopUserDataPaths : IUserDataPaths
{
    public DesktopUserDataPaths(string? installRoot = null, string? userDataRoot = null)
    {
        InstallRoot = installRoot ?? AppContext.BaseDirectory;
        UserDataRoot = userDataRoot
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TinyTBS");
    }

    public string UserDataRoot { get; }
    public string InstallRoot { get; }

    public string Maps => Path.Combine(UserDataRoot, "Maps");
    public string Campaigns => Path.Combine(UserDataRoot, "Campaigns");
    public string Saves => Path.Combine(UserDataRoot, "Saves");
    public string Downloads => Path.Combine(UserDataRoot, "Downloads");
    public string Mods => Path.Combine(InstallRoot, "Mods");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Maps);
        Directory.CreateDirectory(Campaigns);
        Directory.CreateDirectory(Saves);
        Directory.CreateDirectory(Downloads);
        Directory.CreateDirectory(Mods);
    }
}
