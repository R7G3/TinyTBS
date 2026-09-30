using System.IO.Compression;
using TinyTBS.Engine.IO;

namespace TinyTBS.Game.Modules;

/// <summary>
/// Packs a module folder into <c>{UserData}/Downloads/{moduleId}.tinymod.zip</c>
/// (same layout as <see cref="TinymodInstaller"/> expects).
/// </summary>
public sealed class TinymodModuleExporter
{
    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public TinymodModuleExporter(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    /// <summary>
    /// Creates or replaces the zip in Downloads. Returns the full path to the archive.
    /// </summary>
    public string ExportToDownloads(string moduleRoot, string moduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);

        if (!_files.DirectoryExists(moduleRoot))
            throw new TinymodExportException($"Module folder not found: {moduleRoot}");

        var manifestPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!_files.Exists(manifestPath))
        {
            throw new TinymodExportException(
                $"Module folder is missing '{ContentModuleFiles.ModuleJsonFileName}'.");
        }

        ContentModuleManifestParser.ValidateModuleId(moduleId.Trim());
        var id = moduleId.Trim();

        _userDataPaths.EnsureCreated();
        var zipFileName = id + ContentModuleFiles.TinymodZipExtension;
        var destinationPath = _files.Combine(_userDataPaths.Downloads, zipFileName);

        if (_files.Exists(destinationPath))
            _files.DeleteFile(destinationPath);

        try
        {
            ZipFile.CreateFromDirectory(
                moduleRoot,
                destinationPath,
                CompressionLevel.Optimal,
                includeBaseDirectory: false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new TinymodExportException($"Failed to pack module '{id}'.", exception);
        }

        return destinationPath;
    }
}
