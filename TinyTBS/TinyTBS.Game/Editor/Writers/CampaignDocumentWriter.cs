using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes <c>Campaign/campaign.json</c> (+ optional script) and links module.json.</summary>
public sealed class CampaignDocumentWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileSystem _files;

    public CampaignDocumentWriter(IFileSystem files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string Write(
        string scenarioModuleRoot,
        string campaignId,
        string title,
        IReadOnlyList<string> levelIds,
        bool writeScriptIfMissing = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioModuleRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(levelIds);

        ContentModuleManifestParser.ValidateModuleId(campaignId.Trim());

        var campaignRoot = _files.Combine(scenarioModuleRoot, "Campaign");
        _files.CreateDirectory(campaignRoot);

        var chapters = levelIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .Select(id => new CampaignLevelJsonDto
            {
                LevelId = id,
                Path = "Levels/" + id,
            })
            .ToList();

        var payload = new CampaignJsonDto
        {
            FormatVersion = 1,
            Id = campaignId.Trim(),
            Title = title.Trim(),
            Levels = chapters,
        };

        var manifestPath = _files.Combine(campaignRoot, "campaign.json");
        _files.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(payload, WriteOptions) + Environment.NewLine,
            Encoding.UTF8);

        if (writeScriptIfMissing)
        {
            var scriptPath = _files.Combine(campaignRoot, "script.cs");
            if (!_files.Exists(scriptPath))
                _files.WriteAllText(scriptPath, CampaignScriptTemplates.EmptyHooks, Encoding.UTF8);
        }

        ScenarioModuleCampaignLinker.EnsureCampaignContentPath(scenarioModuleRoot, _files);
        return manifestPath;
    }
}
