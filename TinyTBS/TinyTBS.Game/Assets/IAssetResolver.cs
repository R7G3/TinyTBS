namespace TinyTBS.Game.Assets;

/// <summary>
/// Resolves logical asset paths (e.g. Images/units/knight_base.png) to a physical file.
/// Prefers an optional overlay module under Content/Modules/{id}/, then bundled content.
/// Full multi-module composition (scenario + units + theme) comes with content-mods work;
/// this keeps a transitional single-overlay for graphics until then.
/// </summary>
public interface IAssetResolver
{
    /// <summary>
    /// Overlay module folder name under Content/Modules/, or null / empty for bundled only.
    /// </summary>
    string? ActiveModId { get; set; }

    /// <summary>
    /// Returns the best physical path for a logical relative asset path, or null if missing.
    /// </summary>
    string? Resolve(string logicalRelativePath);

    /// <summary>Lists module folder names under Content/Modules/.</summary>
    IReadOnlyList<string> ListMods();
}
