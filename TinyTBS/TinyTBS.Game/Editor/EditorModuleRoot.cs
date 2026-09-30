using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor;

/// <summary>Finds the module folder that owns an editor content file.</summary>
internal static class EditorModuleRoot
{
    /// <summary>Nearest parent directory that contains <c>module.json</c>, or the file's own directory.</summary>
    public static string Find(IFileSystem files, string filePath)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var directory = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrEmpty(directory))
        {
            if (files.Exists(files.Combine(directory, ContentModuleFiles.ModuleJsonFileName)))
                return directory;

            var parent = Path.GetDirectoryName(directory);
            if (parent is null || string.Equals(parent, directory, StringComparison.OrdinalIgnoreCase))
                break;

            directory = parent;
        }

        directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
            throw new EditorException("Content file has no directory: " + filePath);

        return directory;
    }
}
