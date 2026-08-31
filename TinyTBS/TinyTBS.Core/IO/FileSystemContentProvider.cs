namespace TinyTBS.Core.IO;

public sealed class FileSystemContentProvider : IFileContentProvider
{
    public bool Exists(string path) => File.Exists(path);

    public Stream OpenRead(string path) => File.OpenRead(path);

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
