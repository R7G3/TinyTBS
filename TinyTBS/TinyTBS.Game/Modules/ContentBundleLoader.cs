using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Loads a single <c>*.bundle.json</c> file.</summary>
public static class ContentBundleLoader
{
    public static ContentBundleDefinition Load(
        string bundleFilePath,
        IFileSystem files,
        ContentModuleSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleFilePath);
        ArgumentNullException.ThrowIfNull(files);

        if (!files.Exists(bundleFilePath))
            throw new ContentBundleException($"Bundle file not found: {bundleFilePath}");

        using var stream = files.OpenRead(bundleFilePath);
        return ContentBundleJsonParser.Parse(stream, bundleFilePath, source);
    }
}
