using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Modules;
using TinyTBS.Rules;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Ensures scenario <c>module.json</c> has <c>content.campaign</c>.</summary>
public static class ScenarioModuleCampaignLinker
{
    public static void EnsureCampaignContentPath(string scenarioModuleRoot, IFileSystem files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentNullException.ThrowIfNull(files);

        var moduleJsonPath = files.Combine(scenarioModuleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!files.Exists(moduleJsonPath))
            throw new EditorException("module.json not found in scenario root.");

        var text = files.ReadAllText(moduleJsonPath);
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(text);
        }
        catch (JsonException exception)
        {
            throw new EditorException("Failed to parse module.json.", exception);
        }

        if (root is not JsonObject rootObject)
            throw new EditorException("module.json root must be an object.");

        if (rootObject["content"] is not JsonObject contentObject)
        {
            contentObject = new JsonObject();
            rootObject["content"] = contentObject;
        }

        contentObject["campaign"] = CampaignLoader.DefaultManifestRelativePath;

        files.WriteAllText(
            moduleJsonPath,
            rootObject.ToJsonString(ContentJson.Write) + Environment.NewLine,
            Encoding.UTF8);
    }
}
