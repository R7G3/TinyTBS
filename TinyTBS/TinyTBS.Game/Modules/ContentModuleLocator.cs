using TinyTBS.Engine.IO;

namespace TinyTBS.Game.Modules;

/// <summary>
/// Resolves module id → folder root. Prefers user library, then bundled <c>Vanilla/Modules</c>.
/// </summary>
public sealed class ContentModuleLocator
{
    public const string BundledVanillaModulesRelativePath = "Vanilla/Modules";

    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public ContentModuleLocator(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    public string ResolveModuleRoot(string moduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        var trimmedId = moduleId.Trim();

        var userRoot = _files.Combine(_userDataPaths.Modules, trimmedId);
        if (_files.DirectoryExists(userRoot) && HasModuleJson(userRoot))
            return userRoot;

        var bundledRoot = _files.Combine(
            _userDataPaths.InstallRoot,
            BundledVanillaModulesRelativePath,
            trimmedId);
        if (_files.DirectoryExists(bundledRoot) && HasModuleJson(bundledRoot))
            return bundledRoot;

        throw new MatchContentCompositionException(
            $"Module '{trimmedId}' not found in '{_userDataPaths.Modules}' or bundled Vanilla/Modules.");
    }

    private bool HasModuleJson(string moduleRoot) =>
        _files.Exists(_files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName));
}
