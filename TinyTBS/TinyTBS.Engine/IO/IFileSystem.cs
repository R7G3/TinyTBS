using System.Text;

namespace TinyTBS.Engine.IO;

/// <summary>
/// Read, write, list and delete files for game and editor code. Hosts supply a platform store
/// (<see cref="FileSystemContentProvider"/> on desktop); domain code does not call <c>File</c> or <c>Directory</c>.
/// </summary>
public interface IFileSystem : IFileContentProvider
{
    bool DirectoryExists(string path);

    void CreateDirectory(string path);

    /// <summary>Creates or replaces a file and returns a writable stream.</summary>
    Stream Create(string path);

    /// <summary>UTF-8 without a byte-order mark, matching <c>File.WriteAllText(path, text)</c>.</summary>
    void WriteAllText(string path, string contents);

    void WriteAllText(string path, string contents, Encoding encoding);

    string ReadAllText(string path);

    void DeleteFile(string path);

    /// <summary>Deletes the directory and its contents. Does nothing when the directory is already gone.</summary>
    void DeleteDirectory(string path);

    void MoveDirectory(string sourcePath, string destinationPath);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern);

    IEnumerable<string> EnumerateDirectories(string directoryPath);
}
