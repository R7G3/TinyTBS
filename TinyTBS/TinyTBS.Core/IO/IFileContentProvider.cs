namespace TinyTBS.Core.IO;

/// <summary>
/// Opens files by absolute or relative path without hard-coding platform APIs in domain code.
/// </summary>
public interface IFileContentProvider
{
    bool Exists(string path);

    Stream OpenRead(string path);

    /// <summary>
    /// Resolves a path relative to <paramref name="root"/>, or returns absolute paths unchanged.
    /// </summary>
    string Combine(string root, params string[] segments);
}
