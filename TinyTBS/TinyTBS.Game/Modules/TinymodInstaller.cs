using System.IO.Compression;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>
/// Installs <c>.tinymod.zip</c> into <c>{UserData}/Content/Modules/{module.id}/</c>
/// and removes user-library modules.
/// </summary>
public sealed class TinymodInstaller
{
    private readonly IFileContentProvider _files;
    private readonly IUserDataPaths _userDataPaths;

    public TinymodInstaller(IFileContentProvider files, IUserDataPaths userDataPaths)
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

        if (!File.Exists(tinymodZipPath))
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
            using var archive = ZipFile.OpenRead(tinymodZipPath);
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

            DeleteDirectoryIfExists(stagingRoot);
            Directory.CreateDirectory(stagingRoot);

            try
            {
                ExtractArchive(archive, stagingRoot);

                var stagingManifestPath = _files.Combine(stagingRoot, ContentModuleFiles.ModuleJsonFileName);
                if (!_files.Exists(stagingManifestPath))
                {
                    throw new TinymodInstallException(
                        $"Extracted archive is missing '{ContentModuleFiles.ModuleJsonFileName}'.");
                }

                DeleteDirectoryIfExists(destinationRoot);
                Directory.Move(stagingRoot, destinationRoot);
            }
            catch
            {
                DeleteDirectoryIfExists(stagingRoot);
                throw;
            }

            return new ContentModuleInfo
            {
                ModuleId = manifest.ModuleId,
                Type = manifest.Type,
                ContentNamespace = manifest.ContentNamespace,
                Title = manifest.Title,
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
    /// </summary>
    /// <returns><see langword="true"/> if a user folder was removed.</returns>
    public bool Uninstall(string moduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ContentModuleManifestParser.ValidateModuleId(moduleId.Trim());

        var moduleRoot = _files.Combine(_userDataPaths.Modules, moduleId.Trim());
        if (!Directory.Exists(moduleRoot))
            return false;

        try
        {
            Directory.Delete(moduleRoot, recursive: true);
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

    private static void ExtractArchive(ZipArchive archive, string destinationRoot)
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
                Directory.CreateDirectory(directoryPath);
                continue;
            }

            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var targetPath = MapEntryPath(entry.FullName, destinationFullRoot);
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            entry.ExtractToFile(targetPath, overwrite: true);
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

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
