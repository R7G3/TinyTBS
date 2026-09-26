using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>
/// Scans the content-module library: user <c>Modules/</c> and bundled <c>Vanilla/Modules</c>.
/// </summary>
public sealed class ContentModuleLibrary
{
    private readonly IFileContentProvider _files;
    private readonly IUserDataPaths _userDataPaths;

    public ContentModuleLibrary(IFileContentProvider files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    /// <summary>Modules installed under <see cref="IUserDataPaths.Modules"/>.</summary>
    public IReadOnlyList<ContentModuleInfo> ListUserModules() =>
        ScanModulesRoot(_userDataPaths.Modules, ContentModuleSource.UserLibrary);

    /// <summary>Bundled vanilla modules next to the executable.</summary>
    public IReadOnlyList<ContentModuleInfo> ListBundledModules()
    {
        var bundledRoot = _files.Combine(
            AppContext.BaseDirectory,
            ContentModuleLocator.BundledVanillaModulesRelativePath);
        return ScanModulesRoot(bundledRoot, ContentModuleSource.Bundled);
    }

    /// <summary>
    /// User library plus bundled modules not overridden by the same <c>module.id</c> in user data.
    /// </summary>
    public IReadOnlyList<ContentModuleInfo> ListEffectiveModules()
    {
        var userModules = ListUserModules();
        var userIds = new HashSet<string>(
            userModules.Select(module => module.ModuleId),
            StringComparer.Ordinal);

        var result = new List<ContentModuleInfo>(userModules);
        foreach (var bundled in ListBundledModules())
        {
            if (userIds.Contains(bundled.ModuleId))
                continue;
            result.Add(bundled);
        }

        return result
            .OrderBy(module => module.ModuleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<ContentModuleInfo> ScanModulesRoot(string modulesRoot, ContentModuleSource source)
    {
        if (!Directory.Exists(modulesRoot))
            return [];

        var result = new List<ContentModuleInfo>();
        foreach (var moduleDirectory in Directory.GetDirectories(modulesRoot))
        {
            var folderName = Path.GetFileName(moduleDirectory);
            if (string.IsNullOrEmpty(folderName) || folderName.EndsWith(".__installing", StringComparison.OrdinalIgnoreCase))
                continue;

            var moduleJsonPath = _files.Combine(moduleDirectory, ContentModuleFiles.ModuleJsonFileName);
            if (!_files.Exists(moduleJsonPath))
                continue;

            try
            {
                using var stream = _files.OpenRead(moduleJsonPath);
                var info = ContentModuleManifestParser.Parse(stream, moduleDirectory, source);
                result.Add(info);
            }
            catch (TinymodInstallException)
            {
                // Skip corrupt / incomplete folders so one bad install does not break the library.
            }
        }

        return result
            .OrderBy(module => module.ModuleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
