using System.Text.Json;
using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Buildings;

/// <summary>Parses building JSON and buildings <c>module.json</c>.</summary>
public static class BuildingJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static BuildingDefinition ParseBuilding(
        Stream jsonStream,
        string contentNamespace,
        string moduleRootPath)
    {
        BuildingDefinitionDto document;
        try
        {
            document = JsonSerializer.Deserialize<BuildingDefinitionDto>(jsonStream, JsonOptions)
                ?? throw new BuildingLoadException("Building JSON deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new BuildingLoadException("Failed to parse building JSON.", jsonException);
        }

        return FromBuildingDocument(document, contentNamespace, moduleRootPath);
    }

    public static (string ModuleId, string ContentNamespace, string Title, string Version, string BuildingsDir)
        ParseModuleManifest(Stream jsonStream)
    {
        BuildingModuleJsonDto document;
        try
        {
            document = JsonSerializer.Deserialize<BuildingModuleJsonDto>(jsonStream, JsonOptions)
                ?? throw new BuildingLoadException("module.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new BuildingLoadException("Failed to parse buildings module.json.", jsonException);
        }

        if (document.FormatVersion < 1)
            throw new BuildingLoadException($"Unsupported formatVersion '{document.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(document.Id))
            throw new BuildingLoadException("module.json requires non-empty 'id'.");

        if (!string.Equals(document.Type, "buildings", StringComparison.OrdinalIgnoreCase))
            throw new BuildingLoadException($"Expected module type 'buildings', got '{document.Type}'.");

        var contentNamespace = string.IsNullOrWhiteSpace(document.Namespace)
            ? document.Id.Trim()
            : document.Namespace.Trim();

        var buildingsDir = document.Content?.BuildingsDir;
        if (string.IsNullOrWhiteSpace(buildingsDir))
            buildingsDir = "Buildings/";

        var title = string.IsNullOrWhiteSpace(document.Title) ? document.Id.Trim() : document.Title.Trim();
        var version = string.IsNullOrWhiteSpace(document.Version) ? "0.0.0" : document.Version.Trim();

        return (document.Id.Trim(), contentNamespace, title, version, buildingsDir.Trim());
    }

    private static BuildingDefinition FromBuildingDocument(
        BuildingDefinitionDto document,
        string contentNamespace,
        string moduleRootPath)
    {
        if (document.FormatVersion < 1)
            throw new BuildingLoadException($"Unsupported building formatVersion '{document.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(document.Id))
            throw new BuildingLoadException("Building JSON requires non-empty 'id'.");

        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRootPath);

        var localId = document.Id.Trim();
        var contentId = new ContentId(contentNamespace, localId);

        if (document.Sprites is null
            || string.IsNullOrWhiteSpace(document.Sprites.Base)
            || string.IsNullOrWhiteSpace(document.Sprites.Mask))
        {
            throw new BuildingLoadException($"Building '{contentId.Full}' requires sprites.base and sprites.mask.");
        }

        BuildingHealDefinition? heal = null;
        if (document.Heal is not null)
        {
            heal = new BuildingHealDefinition
            {
                Amount = document.Heal.Amount,
                Scope = string.IsNullOrWhiteSpace(document.Heal.Scope) ? "none" : document.Heal.Scope.Trim(),
            };
        }

        BuildingRuinedStatsDefinition? ruined = null;
        if (document.Ruined is not null)
        {
            BuildingHealDefinition? ruinedHeal = null;
            if (document.Ruined.Heal is not null)
            {
                ruinedHeal = new BuildingHealDefinition
                {
                    Amount = document.Ruined.Heal.Amount,
                    Scope = string.IsNullOrWhiteSpace(document.Ruined.Heal.Scope)
                        ? "none"
                        : document.Ruined.Heal.Scope.Trim(),
                };
            }

            ruined = new BuildingRuinedStatsDefinition
            {
                Income = document.Ruined.Income,
                DefenceBonus = document.Ruined.DefenceBonus,
                Heal = ruinedHeal,
                Capturable = document.Ruined.Capturable,
            };
        }

        return new BuildingDefinition
        {
            ContentId = contentId,
            DisplayNameKey = string.IsNullOrWhiteSpace(document.DisplayNameKey)
                ? $"buildings.{localId}"
                : document.DisplayNameKey.Trim(),
            Tags = document.Tags?.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).ToArray() ?? [],
            Sprites = new BuildingSpritesDefinition
            {
                BasePath = document.Sprites.Base.Trim(),
                MaskPath = document.Sprites.Mask.Trim(),
                RuinedBasePath = string.IsNullOrWhiteSpace(document.Sprites.RuinedBase)
                    ? null
                    : document.Sprites.RuinedBase.Trim(),
                RuinedMaskPath = string.IsNullOrWhiteSpace(document.Sprites.RuinedMask)
                    ? null
                    : document.Sprites.RuinedMask.Trim(),
            },
            Income = document.Income,
            DefenceBonus = document.DefenceBonus,
            AllowsRecruit = document.AllowsRecruit,
            RecruitFromTags = document.RecruitFromTags?
                    .Select(tag => tag.Trim())
                    .Where(tag => tag.Length > 0)
                    .ToArray()
                ?? [],
            Heal = heal,
            Capturable = document.Capturable,
            Destroyable = document.Destroyable,
            Repairable = document.Repairable,
            Ruined = ruined,
            CountsTowardPlayerDefeat = document.CountsTowardPlayerDefeat,
            SourceModuleRootPath = moduleRootPath,
        };
    }
}
