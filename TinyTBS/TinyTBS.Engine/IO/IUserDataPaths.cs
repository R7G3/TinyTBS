namespace TinyTBS.Engine.IO;

/// <summary>
/// Platform-specific roots for writable game data and install-adjacent folders.
/// Canonical layout: <c>{UserData}/Content/Modules</c>, <c>Bundles</c>, <c>Saves</c>, <c>Downloads</c>.
/// </summary>
public interface IUserDataPaths
{
    /// <summary>Root for Content/, Saves/, Downloads/.</summary>
    string UserDataRoot { get; }

    /// <summary>Game install / base directory (Desktop: next to executable).</summary>
    string InstallRoot { get; }

    /// <summary><c>{UserData}/Content</c> — modules and bundles.</summary>
    string ContentRoot { get; }

    /// <summary>Installed / user content modules (<c>*.tinymod</c> unpacked as folders).</summary>
    string Modules { get; }

    /// <summary>Preset manifests <c>*.bundle.json</c>.</summary>
    string Bundles { get; }

    string Saves { get; }
    string Downloads { get; }

    /// <summary>Ensures Content/Modules, Bundles, Saves, Downloads exist.</summary>
    void EnsureCreated();
}
