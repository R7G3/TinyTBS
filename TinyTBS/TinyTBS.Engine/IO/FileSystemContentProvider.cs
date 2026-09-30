using System.Text;

namespace TinyTBS.Engine.IO;

/// <summary>Desktop (and any full-trust host) store backed by <see cref="File"/> and <see cref="Directory"/>.</summary>
public sealed class FileSystemContentProvider : IFileSystem
{
    public bool Exists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public Stream OpenRead(string path) => File.OpenRead(path);

    public Stream Create(string path) => File.Create(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);

    public void WriteAllText(string path, string contents, Encoding encoding) =>
        File.WriteAllText(path, contents, encoding);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void DeleteFile(string path) => File.Delete(path);

    public void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    public void MoveDirectory(string sourcePath, string destinationPath) =>
        Directory.Move(sourcePath, destinationPath);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
        File.Copy(sourcePath, destinationPath, overwrite);

    public IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern) =>
        Directory.EnumerateFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);

    public IEnumerable<string> EnumerateDirectories(string directoryPath) =>
        Directory.EnumerateDirectories(directoryPath);

    public string Combine(string root, params string[] segments)
    {
        if (segments.Length == 0)
            return root;

        var parts = new string[segments.Length + 1];
        parts[0] = root;
        Array.Copy(segments, 0, parts, 1, segments.Length);
        return Path.Combine(parts);
    }
}
