namespace TinyTBS.Core.IO;

/// <summary>
/// Platform-specific roots for writable game data and install-adjacent folders.
/// </summary>
public interface IUserDataPaths
{
    /// <summary>Root for Maps, Campaigns, Saves, Downloads.</summary>
    string UserDataRoot { get; }

    /// <summary>Game install / base directory (Desktop: next to executable).</summary>
    string InstallRoot { get; }

    string Maps { get; }
    string Campaigns { get; }
    string Saves { get; }
    string Downloads { get; }

    /// <summary>Mod packs live under InstallRoot/Mods on desktop.</summary>
    string Mods { get; }

    /// <summary>Ensures Maps, Campaigns, Saves, Downloads, Mods directories exist.</summary>
    void EnsureCreated();
}
