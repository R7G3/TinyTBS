using TinyTBS.Engine.IO;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>
/// Scans content-bundle presets: user <c>Bundles/</c> and bundled <c>Vanilla/Bundles</c>.
/// </summary>
public sealed class ContentBundleLibrary
{
    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public ContentBundleLibrary(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    public IReadOnlyList<ContentBundleDefinition> ListUserBundles() =>
        ScanBundlesRoot(_userDataPaths.Bundles, ContentModuleSource.UserLibrary);

    public IReadOnlyList<ContentBundleDefinition> ListBundledBundles()
    {
        var bundledRoot = _files.Combine(
            _userDataPaths.InstallRoot,
            ContentBundleLocator.BundledVanillaBundlesRelativePath);
        return ScanBundlesRoot(bundledRoot, ContentModuleSource.Bundled);
    }

    /// <summary>
    /// User library plus bundled presets not overridden by the same <c>bundle.id</c> in user data.
    /// </summary>
    public IReadOnlyList<ContentBundleDefinition> ListEffectiveBundles()
    {
        var userBundles = ListUserBundles();
        var userIds = new HashSet<string>(
            userBundles.Select(bundle => bundle.BundleId),
            StringComparer.Ordinal);

        var result = new List<ContentBundleDefinition>(userBundles);
        foreach (var bundled in ListBundledBundles())
        {
            if (userIds.Contains(bundled.BundleId))
                continue;
            result.Add(bundled);
        }

        return result
            .OrderBy(bundle => bundle.BundleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<ContentBundleDefinition> ScanBundlesRoot(
        string bundlesRoot,
        ContentModuleSource source)
    {
        if (!_files.DirectoryExists(bundlesRoot))
            return [];

        var result = new List<ContentBundleDefinition>();
        foreach (var filePath in _files.EnumerateFiles(bundlesRoot, "*" + ContentBundleFiles.BundleJsonExtension))
        {
            try
            {
                result.Add(ContentBundleLoader.Load(filePath, _files, source));
            }
            catch (ContentBundleException)
            {
                // Skip corrupt presets so one bad file does not break the library.
            }
        }

        return result
            .OrderBy(bundle => bundle.BundleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
