namespace TinyTBS.Engine.IO;

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

    public string ContentRoot => Path.Combine(UserDataRoot, "Content");
    public string Modules => Path.Combine(ContentRoot, "Modules");
    public string Bundles => Path.Combine(ContentRoot, "Bundles");
    public string Saves => Path.Combine(UserDataRoot, "Saves");
    public string Downloads => Path.Combine(UserDataRoot, "Downloads");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Modules);
        Directory.CreateDirectory(Bundles);
        Directory.CreateDirectory(Saves);
        Directory.CreateDirectory(Downloads);
    }
}
