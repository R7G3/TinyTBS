using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns;
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

    private readonly IFileContentProvider _files;

    public CampaignDocumentWriter(IFileContentProvider files)
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
        Directory.CreateDirectory(campaignRoot);

        var chapters = levelIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .Select(id => new CampaignLevelWriteDto
            {
                LevelId = id,
                Path = "Levels/" + id,
            })
            .ToList();

        var payload = new CampaignWriteDto
        {
            FormatVersion = 1,
            Id = campaignId.Trim(),
            Title = title.Trim(),
            Levels = chapters,
        };

        var manifestPath = _files.Combine(campaignRoot, "campaign.json");
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(payload, WriteOptions) + Environment.NewLine,
            Encoding.UTF8);

        if (writeScriptIfMissing)
        {
            var scriptPath = _files.Combine(campaignRoot, "script.cs");
            if (!File.Exists(scriptPath))
                File.WriteAllText(scriptPath, CampaignScriptTemplates.EmptyHooks, Encoding.UTF8);
        }

        ScenarioModuleCampaignLinker.EnsureCampaignContentPath(scenarioModuleRoot, _files);
        return manifestPath;
    }

    private sealed class CampaignWriteDto
    {
        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("levels")]
        public List<CampaignLevelWriteDto> Levels { get; set; } = [];
    }

    private sealed class CampaignLevelWriteDto
    {
        [JsonPropertyName("levelId")]
        public string LevelId { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }
}
