using System.Text.Json;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Units;

/// <summary>Parses unit JSON and units <c>module.json</c>.</summary>
public static class UnitJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static UnitDefinition ParseUnit(Stream jsonStream, string contentNamespace, string moduleRootPath)
    {
        UnitDefinitionDto document;
        try
        {
            document = JsonSerializer.Deserialize<UnitDefinitionDto>(jsonStream, JsonOptions)
                ?? throw new UnitLoadException("Unit JSON deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new UnitLoadException("Failed to parse unit JSON.", jsonException);
        }

        return FromUnitDocument(document, contentNamespace, moduleRootPath);
    }

    public static (string ModuleId, string ContentNamespace, string Title, string Version, string UnitsDir, IReadOnlyList<ContentId> RecruitPool)
        ParseModuleManifest(Stream jsonStream)
    {
        UnitModuleJsonDto document;
        try
        {
            document = JsonSerializer.Deserialize<UnitModuleJsonDto>(jsonStream, JsonOptions)
                ?? throw new UnitLoadException("module.json deserialized to null.");
        }
        catch (JsonException jsonException)
        {
            throw new UnitLoadException("Failed to parse units module.json.", jsonException);
        }

        if (document.FormatVersion < 1)
            throw new UnitLoadException($"Unsupported formatVersion '{document.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(document.Id))
            throw new UnitLoadException("module.json requires non-empty 'id'.");

        if (!string.Equals(document.Type, "units", StringComparison.OrdinalIgnoreCase))
            throw new UnitLoadException($"Expected module type 'units', got '{document.Type}'.");

        var contentNamespace = string.IsNullOrWhiteSpace(document.Namespace)
            ? document.Id.Trim()
            : document.Namespace.Trim();

        var unitsDir = document.Content?.UnitsDir;
        if (string.IsNullOrWhiteSpace(unitsDir))
            unitsDir = "Units/";

        var recruitPool = new List<ContentId>();
        if (document.Recruit?.AddsToPool is { Count: > 0 } poolEntries)
        {
            foreach (var entry in poolEntries)
            {
                if (!ContentId.TryParse(entry, out var contentId))
                    throw new UnitLoadException($"Invalid recruit pool id '{entry}'.");
                recruitPool.Add(contentId);
            }
        }

        var title = string.IsNullOrWhiteSpace(document.Title) ? document.Id.Trim() : document.Title.Trim();
        var version = string.IsNullOrWhiteSpace(document.Version) ? "0.0.0" : document.Version.Trim();

        return (document.Id.Trim(), contentNamespace, title, version, unitsDir.Trim(), recruitPool);
    }

    private static UnitDefinition FromUnitDocument(
        UnitDefinitionDto document,
        string contentNamespace,
        string moduleRootPath)
    {
        if (document.FormatVersion < 1)
            throw new UnitLoadException($"Unsupported unit formatVersion '{document.FormatVersion}'.");

        if (string.IsNullOrWhiteSpace(document.Id))
            throw new UnitLoadException("Unit JSON requires non-empty 'id'.");

        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRootPath);

        var localId = document.Id.Trim();
        var contentId = new ContentId(contentNamespace, localId);

        if (document.MaxHealth <= 0)
            throw new UnitLoadException($"Unit '{contentId.Full}' requires positive maxHealth.");

        if (document.AttackRangeMin < 0 || document.AttackRangeMax < document.AttackRangeMin)
        {
            throw new UnitLoadException(
                $"Unit '{contentId.Full}' has invalid attack range {document.AttackRangeMin}–{document.AttackRangeMax}.");
        }

        UnitSpritesDefinition? sprites = null;
        if (document.Sprites is not null)
        {
            if (string.IsNullOrWhiteSpace(document.Sprites.Base) || string.IsNullOrWhiteSpace(document.Sprites.Mask))
                throw new UnitLoadException($"Unit '{contentId.Full}' sprites require base and mask paths.");

            sprites = new UnitSpritesDefinition
            {
                BasePath = document.Sprites.Base.Trim(),
                MaskPath = document.Sprites.Mask.Trim(),
            };
        }

        var abilities = new List<UnitAbilityDefinition>();
        if (document.Abilities is not null)
        {
            foreach (var abilityEntry in document.Abilities)
            {
                if (string.IsNullOrWhiteSpace(abilityEntry.Type))
                    throw new UnitLoadException($"Unit '{contentId.Full}' has an ability without type.");

                abilities.Add(new UnitAbilityDefinition
                {
                    Type = abilityEntry.Type.Trim(),
                    Amount = abilityEntry.Amount,
                    MinRange = abilityEntry.MinRange,
                    Value = abilityEntry.Value,
                    Radius = abilityEntry.Radius,
                    Tags = abilityEntry.Tags?.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).ToArray()
                        ?? [],
                });
            }
        }

        return new UnitDefinition
        {
            ContentId = contentId,
            DisplayNameKey = string.IsNullOrWhiteSpace(document.DisplayNameKey)
                ? $"units.{localId}"
                : document.DisplayNameKey.Trim(),
            MovementClass = string.IsNullOrWhiteSpace(document.MovementClass)
                ? "foot"
                : document.MovementClass.Trim(),
            Tags = document.Tags?.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).ToArray() ?? [],
            Recruitable = document.Recruitable ?? true,
            Attack = document.Attack,
            Defence = document.Defence,
            MaxHealth = document.MaxHealth,
            AttackRangeMin = document.AttackRangeMin,
            AttackRangeMax = document.AttackRangeMax,
            Speed = document.Speed,
            Cost = document.Cost,
            Abilities = abilities,
            LeavesGravestone = document.LeavesGravestone ?? true,
            Sprites = sprites,
            SourceModuleRootPath = moduleRootPath,
        };
    }
}
