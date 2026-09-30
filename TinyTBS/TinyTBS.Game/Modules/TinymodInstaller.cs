using System.IO.Compression;
using TinyTBS.Engine.IO;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>
/// Installs <c>.tinymod.zip</c> into <c>{UserData}/Content/Modules/{module.id}/</c>
/// and removes user-library modules.
/// </summary>
public sealed class TinymodInstaller
{
    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public TinymodInstaller(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    /// <summary>
    /// Unpacks a tinymod zip into the user library. Replaces an existing folder with the same <c>module.id</c>.
    /// </summary>
    public ContentModuleInfo Install(string tinymodZipPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tinymodZipPath);

        if (!_files.Exists(tinymodZipPath))
            throw new TinymodInstallException($"Tinymod archive not found: {tinymodZipPath}");

        if (!HasTinymodExtension(tinymodZipPath))
        {
            throw new TinymodInstallException(
                $"Expected a '{ContentModuleFiles.TinymodZipExtension}' file, got '{Path.GetFileName(tinymodZipPath)}'.");
        }

        _userDataPaths.EnsureCreated();

        ContentModuleInfo manifest;
        try
        {
            using var zipStream = _files.OpenRead(tinymodZipPath);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var moduleJsonEntry = FindRootModuleJson(archive)
                ?? throw new TinymodInstallException(
                    $"Archive must contain '{ContentModuleFiles.ModuleJsonFileName}' at the zip root.");

            using (var manifestStream = moduleJsonEntry.Open())
            {
                // Root path filled after extract; temporary placeholder for validation.
                manifest = ContentModuleManifestParser.Parse(
                    manifestStream,
                    moduleRootPath: ".",
                    ContentModuleSource.UserLibrary);
            }

            var destinationRoot = _files.Combine(_userDataPaths.Modules, manifest.ModuleId);
            var stagingRoot = destinationRoot + ".__installing";

            _files.DeleteDirectory(stagingRoot);
            _files.CreateDirectory(stagingRoot);

            try
            {
                ExtractArchive(archive, stagingRoot);

                var stagingManifestPath = _files.Combine(stagingRoot, ContentModuleFiles.ModuleJsonFileName);
                if (!_files.Exists(stagingManifestPath))
                {
                    throw new TinymodInstallException(
                        $"Extracted archive is missing '{ContentModuleFiles.ModuleJsonFileName}'.");
                }

                _files.DeleteDirectory(destinationRoot);
                _files.MoveDirectory(stagingRoot, destinationRoot);
            }
            catch
            {
                _files.DeleteDirectory(stagingRoot);
                throw;
            }

            return new ContentModuleInfo
            {
                ModuleId = manifest.ModuleId,
                Type = manifest.Type,
                ContentNamespace = manifest.ContentNamespace,
                Title = manifest.Title,
                Description = manifest.Description,
                Version = manifest.Version,
                ModuleRootPath = destinationRoot,
                Source = ContentModuleSource.UserLibrary,
            };
        }
        catch (TinymodInstallException)
        {
            throw;
        }
        catch (InvalidDataException invalidDataException)
        {
            throw new TinymodInstallException(
                $"Invalid zip archive: {tinymodZipPath}",
                invalidDataException);
        }
        catch (IOException ioException)
        {
            throw new TinymodInstallException(
                $"Failed to install tinymod '{tinymodZipPath}'.",
                ioException);
        }
    }

    /// <summary>
    /// Removes a module folder from the user library. Bundled vanilla is never deleted.
    /// Also removes a matching <c>{moduleId}.tinymod.zip</c> from Downloads if present
    /// (editor Export / install queue leftover).
    /// </summary>
    /// <returns><see langword="true"/> if a user folder was removed.</returns>
    public bool Uninstall(string moduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ContentModuleManifestParser.ValidateModuleId(moduleId.Trim());

        var id = moduleId.Trim();
        var moduleRoot = _files.Combine(_userDataPaths.Modules, id);
        if (!_files.DirectoryExists(moduleRoot))
            return false;

        try
        {
            _files.DeleteDirectory(moduleRoot);
            TryDeleteDownloadArchive(id);
            return true;
        }
        catch (IOException ioException)
        {
            throw new TinymodInstallException(
                $"Failed to uninstall module '{moduleId}'.",
                ioException);
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            throw new TinymodInstallException(
                $"Failed to uninstall module '{moduleId}'.",
                unauthorizedAccessException);
        }
    }

    /// <summary>
    /// Deletes <c>{UserData}/Downloads/{moduleId}.tinymod.zip</c> if it exists.
    /// Best-effort: I/O errors are ignored so uninstall still succeeds.
    /// </summary>
    public void TryDeleteDownloadArchive(string moduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        var id = moduleId.Trim();
        var zipPath = _files.Combine(
            _userDataPaths.Downloads,
            id + ContentModuleFiles.TinymodZipExtension);
        try
        {
            if (_files.Exists(zipPath))
                _files.DeleteFile(zipPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>Lists <c>*.tinymod.zip</c> archives waiting in <see cref="IUserDataPaths.Downloads"/>.</summary>
    public IReadOnlyList<string> ListDownloadArchives()
    {
        _userDataPaths.EnsureCreated();
        if (!_files.DirectoryExists(_userDataPaths.Downloads))
            return [];

        return _files.EnumerateFiles(_userDataPaths.Downloads, "*" + ContentModuleFiles.TinymodZipExtension)
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool HasTinymodExtension(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.EndsWith(ContentModuleFiles.TinymodZipExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static ZipArchiveEntry? FindRootModuleJson(ZipArchive archive)
    {
        foreach (var entry in archive.Entries)
        {
            if (string.Equals(entry.FullName, ContentModuleFiles.ModuleJsonFileName, StringComparison.OrdinalIgnoreCase))
                return entry;

            // Some tools store "./module.json"
            if (string.Equals(entry.Name, ContentModuleFiles.ModuleJsonFileName, StringComparison.OrdinalIgnoreCase)
                && !entry.FullName.Contains('/', StringComparison.Ordinal)
                && !entry.FullName.Contains('\\', StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private void ExtractArchive(ZipArchive archive, string destinationRoot)
    {
        var destinationFullRoot = Path.GetFullPath(destinationRoot);

        foreach (var entry in archive.Entries)
        {
            var isDirectoryEntry = string.IsNullOrEmpty(entry.Name)
                || entry.FullName.EndsWith('/')
                || entry.FullName.EndsWith('\\');
            if (isDirectoryEntry && string.IsNullOrEmpty(entry.Name))
            {
                var directoryPath = MapEntryPath(entry.FullName, destinationFullRoot);
                _files.CreateDirectory(directoryPath);
                continue;
            }

            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var targetPath = MapEntryPath(entry.FullName, destinationFullRoot);
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDirectory))
                _files.CreateDirectory(targetDirectory);

            using (var source = entry.Open())
            using (var destination = _files.Create(targetPath))
                source.CopyTo(destination);
        }
    }

    private static string MapEntryPath(string entryFullName, string destinationFullRoot)
    {
        var relativePath = entryFullName
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        if (string.IsNullOrEmpty(relativePath)
            || relativePath.Contains(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relativePath == "..")
        {
            throw new TinymodInstallException($"Zip entry path is unsafe: '{entryFullName}'.");
        }

        var combined = Path.GetFullPath(Path.Combine(destinationFullRoot, relativePath));
        var rootWithSeparator = destinationFullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? destinationFullRoot
            : destinationFullRoot + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(combined, destinationFullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new TinymodInstallException($"Zip entry escapes destination: '{entryFullName}'.");
        }

        return combined;
    }

}
