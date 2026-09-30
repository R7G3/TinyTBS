using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Campaigns;

/// <summary>Reads/writes <c>campaign_*.json</c> progress files.</summary>
public sealed class CampaignProgressStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public CampaignProgressStore(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    public string? FindLatestPathForCampaign(string scenarioModuleId, string campaignId)
    {
        foreach (var entry in ListNewestFirst())
        {
            if (string.Equals(entry.ScenarioModuleId, scenarioModuleId, StringComparison.Ordinal)
                && string.Equals(entry.CampaignId, campaignId, StringComparison.Ordinal))
            {
                return entry.FilePath;
            }
        }

        return null;
    }

    public CampaignProgressDocument? TryLoadLatestForCampaign(string scenarioModuleId, string campaignId)
    {
        var path = FindLatestPathForCampaign(scenarioModuleId, campaignId);
        return path is null ? null : ReadFile(path);
    }

    public CampaignProgressDocument ReadFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!_files.Exists(filePath))
            throw new MatchContentCompositionException($"Campaign save not found: {filePath}");

        try
        {
            using var stream = _files.OpenRead(filePath);
            var document = JsonSerializer.Deserialize<CampaignProgressDocument>(stream, JsonOptions)
                ?? throw new MatchContentCompositionException("Campaign save deserialized to null.");
            return Normalize(document);
        }
        catch (JsonException jsonException)
        {
            throw new MatchContentCompositionException("Failed to parse campaign save.", jsonException);
        }
    }

    public string Write(CampaignProgressDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _userDataPaths.EnsureCreated();

        var normalized = Normalize(document);
        // One canonical file per campaign id (overwrite) so progress stays stable.
        var safeId = SanitizeFileStem(normalized.CampaignId);
        var filePath = Path.Combine(_userDataPaths.Saves, $"campaign_{safeId}.json");
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        _files.WriteAllText(filePath, json);
        return filePath;
    }

    public IReadOnlyList<CampaignProgressListEntry> ListNewestFirst()
    {
        _userDataPaths.EnsureCreated();
        if (!_files.DirectoryExists(_userDataPaths.Saves))
            return [];

        var list = new List<CampaignProgressListEntry>();
        foreach (var path in _files.EnumerateFiles(_userDataPaths.Saves, "campaign_*.json"))
        {
            try
            {
                var document = ReadFile(path);
                list.Add(new CampaignProgressListEntry
                {
                    FilePath = path,
                    WrittenAtUtc = document.WrittenAtUtc,
                    CampaignId = document.CampaignId,
                    ScenarioModuleId = document.ScenarioModuleId,
                    CurrentLevelId = document.CurrentLevelId,
                    Title = document.CampaignTitle ?? document.CampaignId,
                });
            }
            catch (Exception)
            {
                // Skip corrupt.
            }
        }

        return list
            .OrderByDescending(entry => entry.WrittenAtUtc)
            .ThenByDescending(entry => entry.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool Delete(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var fullPath = Path.GetFullPath(filePath);
        var savesRoot = Path.GetFullPath(_userDataPaths.Saves);
        if (!fullPath.StartsWith(savesRoot, StringComparison.OrdinalIgnoreCase))
            throw new MatchContentCompositionException("Refusing to delete outside Saves.");

        if (!_files.Exists(fullPath))
            return false;

        _files.DeleteFile(fullPath);
        return true;
    }

    private static CampaignProgressDocument Normalize(CampaignProgressDocument document)
    {
        if (document.SaveVersion < 1)
            throw new MatchContentCompositionException($"Unsupported campaign saveVersion '{document.SaveVersion}'.");
        if (!string.Equals(document.Kind, CampaignProgressDocument.KindCampaign, StringComparison.OrdinalIgnoreCase))
            throw new MatchContentCompositionException($"Expected kind '{CampaignProgressDocument.KindCampaign}'.");
        if (string.IsNullOrWhiteSpace(document.CampaignId))
            throw new MatchContentCompositionException("campaignId is required.");
        if (string.IsNullOrWhiteSpace(document.ScenarioModuleId))
            throw new MatchContentCompositionException("scenarioModuleId is required.");
        if (string.IsNullOrWhiteSpace(document.CurrentLevelId))
            throw new MatchContentCompositionException("currentLevelId is required.");

        var unlocked = document.UnlockedLevelIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        if (!unlocked.Contains(document.CurrentLevelId.Trim(), StringComparer.Ordinal))
            unlocked.Insert(0, document.CurrentLevelId.Trim());

        return new CampaignProgressDocument
        {
            SaveVersion = document.SaveVersion,
            Kind = CampaignProgressDocument.KindCampaign,
            WrittenAtUtc = document.WrittenAtUtc == default
                ? DateTimeOffset.UtcNow
                : document.WrittenAtUtc.ToUniversalTime(),
            CampaignId = document.CampaignId.Trim(),
            ScenarioModuleId = document.ScenarioModuleId.Trim(),
            CampaignTitle = string.IsNullOrWhiteSpace(document.CampaignTitle)
                ? null
                : document.CampaignTitle.Trim(),
            CurrentLevelId = document.CurrentLevelId.Trim(),
            UnlockedLevelIds = unlocked,
            PendingNextLevelId = string.IsNullOrWhiteSpace(document.PendingNextLevelId)
                ? null
                : document.PendingNextLevelId.Trim(),
            UnitCap = document.UnitCap,
            ContentSetup = document.ContentSetup,
            PlayerSeats = document.PlayerSeats ?? [],
            Extensions = document.Extensions is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(document.Extensions, StringComparer.Ordinal),
        };
    }

    private static string SanitizeFileStem(string id)
    {
        var chars = id.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_').ToArray();
        return new string(chars);
    }
}
