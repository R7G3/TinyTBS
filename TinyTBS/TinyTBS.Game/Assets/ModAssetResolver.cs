using TinyTBS.Engine.IO;

namespace TinyTBS.Game.Assets;

/// <summary>
/// Resolves assets from an optional overlay module folder, then bundled content.
/// Looks under <c>Modules/{id}/Resources/</c> and <c>Modules/{id}/</c> (legacy-style Images/).
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

        var relative = logicalRelativePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        if (!string.IsNullOrWhiteSpace(ActiveModId))
        {
            var underResources = _files.Combine(_paths.Modules, ActiveModId, "Resources", relative);
            if (_files.Exists(underResources))
                return underResources;

            var underModuleRoot = _files.Combine(_paths.Modules, ActiveModId, relative);
            if (_files.Exists(underModuleRoot))
                return underModuleRoot;
        }

        var bundled = _files.Combine(_bundledContentRoot, relative);
        return _files.Exists(bundled) ? bundled : null;
    }

    public IReadOnlyList<string> ListMods()
    {
        if (!Directory.Exists(_paths.Modules))
            return Array.Empty<string>();

        return Directory.GetDirectories(_paths.Modules)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
