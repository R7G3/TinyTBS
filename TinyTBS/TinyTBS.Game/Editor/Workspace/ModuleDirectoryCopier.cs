using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Workspace;

/// <summary>
/// Copy-on-write: duplicate a module folder into the user library (never mutates InstallRoot).
/// </summary>
public static class ModuleDirectoryCopier
{
    public static string CopyToUserLibrary(
        string sourceModuleRoot,
        string targetModuleId,
        IFileSystem files,
        IUserDataPaths userDataPaths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetModuleId);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(userDataPaths);

        ContentModuleManifestParser.ValidateModuleId(targetModuleId.Trim());
        var moduleId = targetModuleId.Trim();

        if (!files.DirectoryExists(sourceModuleRoot))
            throw new EditorException($"Source module folder not found: {sourceModuleRoot}");

        userDataPaths.EnsureCreated();
        var destinationRoot = files.Combine(userDataPaths.Modules, moduleId);
        if (files.DirectoryExists(destinationRoot))
            throw new EditorException($"User module '{moduleId}' already exists.");

        try
        {
            CopyDirectoryRecursive(files, sourceModuleRoot, destinationRoot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            TryDeleteDirectory(files, destinationRoot);
            throw new EditorException($"Failed to copy module to '{moduleId}'.", exception);
        }

        var moduleJsonPath = files.Combine(destinationRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!files.Exists(moduleJsonPath))
        {
            TryDeleteDirectory(files, destinationRoot);
            throw new EditorException("Copied folder is missing module.json.");
        }

        return destinationRoot;
    }

    private static void CopyDirectoryRecursive(IFileSystem files, string sourceRoot, string destinationRoot)
    {
        files.CreateDirectory(destinationRoot);

        foreach (var filePath in files.EnumerateFiles(sourceRoot, "*"))
        {
            var fileName = Path.GetFileName(filePath);
            files.CopyFile(filePath, Path.Combine(destinationRoot, fileName), overwrite: false);
        }

        foreach (var directoryPath in files.EnumerateDirectories(sourceRoot))
        {
            var directoryName = Path.GetFileName(directoryPath);
            if (string.IsNullOrEmpty(directoryName))
                continue;

            CopyDirectoryRecursive(files, directoryPath, Path.Combine(destinationRoot, directoryName));
        }
    }

    private static void TryDeleteDirectory(IFileSystem files, string path)
    {
        try
        {
            if (files.DirectoryExists(path))
                files.DeleteDirectory(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
