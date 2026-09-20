using TinyTBS.Engine.IO;

namespace TinyTBS.Game.Assets;

/// <summary>
/// Resolves assets from an optional overlay module folder, then bundled content.
/// Lookup order for an active module: <c>Modules/{id}/Resources/</c>, then <c>Modules/{id}/</c>.
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

        var relativePath = logicalRelativePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        if (!string.IsNullOrWhiteSpace(ActiveModId))
        {
            var underResources = _files.Combine(_paths.Modules, ActiveModId, "Resources", relativePath);
            if (_files.Exists(underResources))
                return underResources;

            var underModuleRoot = _files.Combine(_paths.Modules, ActiveModId, relativePath);
            if (_files.Exists(underModuleRoot))
                return underModuleRoot;
        }

        var bundledPath = _files.Combine(_bundledContentRoot, relativePath);
        return _files.Exists(bundledPath) ? bundledPath : null;
    }

    public IReadOnlyList<string> ListMods()
    {
        if (!Directory.Exists(_paths.Modules))
            return [];

        return Directory.GetDirectories(_paths.Modules)
            .Select(Path.GetFileName)
            .Where(folderName => !string.IsNullOrEmpty(folderName))
            .Cast<string>()
            .OrderBy(folderName => folderName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
