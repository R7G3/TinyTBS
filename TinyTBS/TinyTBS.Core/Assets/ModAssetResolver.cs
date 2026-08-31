using TinyTBS.Core.IO;

namespace TinyTBS.Core.Assets;

/// <summary>
/// Vanilla-first resolver: optional active mod under Mods/{id}/, then bundled content root.
/// </summary>
public sealed class ModAssetResolver : IAssetResolver
{
    private readonly IUserDataPaths _paths;
    private readonly IFileContentProvider _files;
    private readonly string _bundledContentRoot;

    public ModAssetResolver(
        IUserDataPaths paths,
        IFileContentProvider files,
        string bundledContentRoot)
    {
        _paths = paths;
        _files = files;
        _bundledContentRoot = bundledContentRoot;
    }

    public string? ActiveModId { get; set; }

    public string? Resolve(string logicalRelativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalRelativePath);

        var normalized = logicalRelativePath.Replace('\\', '/').TrimStart('/');

        if (!string.IsNullOrWhiteSpace(ActiveModId))
        {
            var modPath = _files.Combine(_paths.Mods, ActiveModId, normalized.Replace('/', Path.DirectorySeparatorChar));
            if (_files.Exists(modPath))
                return modPath;
        }

        var bundled = _files.Combine(_bundledContentRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
        return _files.Exists(bundled) ? bundled : null;
    }

    public IReadOnlyList<string> ListMods()
    {
        if (!Directory.Exists(_paths.Mods))
            return Array.Empty<string>();

        return Directory.GetDirectories(_paths.Mods)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
