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
        IFileContentProvider files,
        IUserDataPaths userDataPaths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModuleRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetModuleId);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(userDataPaths);

        ContentModuleManifestParser.ValidateModuleId(targetModuleId.Trim());
        var moduleId = targetModuleId.Trim();

        if (!Directory.Exists(sourceModuleRoot))
            throw new EditorException($"Source module folder not found: {sourceModuleRoot}");

        userDataPaths.EnsureCreated();
        var destinationRoot = files.Combine(userDataPaths.Modules, moduleId);
        if (Directory.Exists(destinationRoot))
            throw new EditorException($"User module '{moduleId}' already exists.");

        try
        {
            CopyDirectoryRecursive(sourceModuleRoot, destinationRoot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            TryDeleteDirectory(destinationRoot);
            throw new EditorException($"Failed to copy module to '{moduleId}'.", exception);
        }

        var moduleJsonPath = files.Combine(destinationRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!files.Exists(moduleJsonPath))
        {
            TryDeleteDirectory(destinationRoot);
            throw new EditorException("Copied folder is missing module.json.");
        }

        return destinationRoot;
    }

    private static void CopyDirectoryRecursive(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);

        foreach (var filePath in Directory.GetFiles(sourceRoot))
        {
            var fileName = Path.GetFileName(filePath);
            File.Copy(filePath, Path.Combine(destinationRoot, fileName), overwrite: false);
        }

        foreach (var directoryPath in Directory.GetDirectories(sourceRoot))
        {
            var directoryName = Path.GetFileName(directoryPath);
            if (string.IsNullOrEmpty(directoryName))
                continue;

            CopyDirectoryRecursive(directoryPath, Path.Combine(destinationRoot, directoryName));
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
