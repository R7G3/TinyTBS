using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>
/// Resolves bundle id → <c>{id}.bundle.json</c>. Prefers user library, then bundled <c>Vanilla/Bundles</c>.
/// </summary>
public sealed class ContentBundleLocator
{
    public const string BundledVanillaBundlesRelativePath = "Vanilla/Bundles";

    private readonly IFileContentProvider _files;
    private readonly IUserDataPaths _userDataPaths;

    public ContentBundleLocator(IFileContentProvider files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    public string ResolveBundleFilePath(string bundleId) =>
        Resolve(bundleId).FilePath;

    public ContentBundleDefinition Load(string bundleId)
    {
        var (filePath, source) = Resolve(bundleId);
        return ContentBundleLoader.Load(filePath, _files, source);
    }

    private (string FilePath, ContentModuleSource Source) Resolve(string bundleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleId);
        var trimmedId = bundleId.Trim();
        var fileName = trimmedId + ContentBundleFiles.BundleJsonExtension;

        var userPath = _files.Combine(_userDataPaths.Bundles, fileName);
        if (_files.Exists(userPath))
            return (userPath, ContentModuleSource.UserLibrary);

        var bundledPath = _files.Combine(
            AppContext.BaseDirectory,
            BundledVanillaBundlesRelativePath,
            fileName);
        if (_files.Exists(bundledPath))
            return (bundledPath, ContentModuleSource.Bundled);

        throw new ContentBundleException(
            $"Bundle '{trimmedId}' not found in '{_userDataPaths.Bundles}' or bundled Vanilla/Bundles.");
    }
}
