using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Updates <c>id</c> / <c>namespace</c> / optional <c>title</c> in an existing module.json.</summary>
public static class ModuleManifestIdRewriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    public static void RewriteIdentity(
        string moduleRootPath,
        string moduleId,
        IFileContentProvider files,
        string? title = null,
        bool forceNamespaceToModuleId = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentNullException.ThrowIfNull(files);

        ContentModuleManifestParser.ValidateModuleId(moduleId.Trim());
        var id = moduleId.Trim();
        var moduleJsonPath = files.Combine(moduleRootPath, ContentModuleFiles.ModuleJsonFileName);
        if (!files.Exists(moduleJsonPath))
            throw new EditorException($"module.json not found at '{moduleJsonPath}'.");

        JsonNode root;
        try
        {
            var text = File.ReadAllText(moduleJsonPath);
            root = JsonNode.Parse(text)
                ?? throw new EditorException("module.json parsed to null.");
        }
        catch (JsonException jsonException)
        {
            throw new EditorException("Failed to parse module.json for id rewrite.", jsonException);
        }

        root["id"] = id;
        if (forceNamespaceToModuleId)
            root["namespace"] = id;
        if (!string.IsNullOrWhiteSpace(title))
            root["title"] = title.Trim();

        File.WriteAllText(moduleJsonPath, root.ToJsonString(WriteOptions) + Environment.NewLine, Encoding.UTF8);
    }
}
