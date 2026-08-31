namespace TinyTBS.Core.Assets;

/// <summary>
/// Resolves logical asset paths (e.g. Images/units/knight_base.png) to a physical file,
/// preferring the active mod and falling back to bundled content.
/// </summary>
public interface IAssetResolver
{
    /// <summary>Active mod folder name under Mods/, or null / empty for vanilla only.</summary>
    string? ActiveModId { get; set; }

    /// <summary>
    /// Returns the best physical path for a logical relative asset path, or null if missing.
    /// </summary>
    string? Resolve(string logicalRelativePath);

    /// <summary>Lists available mod folder names under Mods/.</summary>
    IReadOnlyList<string> ListMods();
}
