using System.Text.Json;
using TinyTBS.Rules.Maps.Models;
using TinyTBS.Rules.Modules.Models;
using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Rules.Units;

/// <summary>Parses unit JSON and units <c>module.json</c>.</summary>
public static class UnitJsonParser
{
    public static UnitDefinition ParseUnit(Stream jsonStream, string contentNamespace, string moduleRootPath)
    {
        UnitDefinitionDto document;
        try
        {
            document = JsonSerializer.Deserialize<UnitDefinitionDto>(jsonStream, ContentJson.Read)
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
            document = JsonSerializer.Deserialize<UnitModuleJsonDto>(jsonStream, ContentJson.Read)
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

        if (!string.Equals(document.Type, ContentModuleTypeIds.Units, StringComparison.OrdinalIgnoreCase))
            throw new UnitLoadException($"Expected module type '{ContentModuleTypeIds.Units}', got '{document.Type}'.");

        var moduleId = document.Id;
        var contentNamespace = string.IsNullOrWhiteSpace(document.Namespace)
            ? moduleId
            : document.Namespace;

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

        var title = string.IsNullOrWhiteSpace(document.Title) ? moduleId : document.Title;
        var version = string.IsNullOrWhiteSpace(document.Version) ? "0.0.0" : document.Version;

        return (moduleId, contentNamespace, title, version, unitsDir, recruitPool);
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

        var localId = document.Id;
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
                BasePath = document.Sprites.Base,
                MaskPath = document.Sprites.Mask,
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
                    Type = abilityEntry.Type,
                    Amount = abilityEntry.Amount,
                    MinRange = abilityEntry.MinRange,
                    Value = abilityEntry.Value,
                    Radius = abilityEntry.Radius,
                    Tags = abilityEntry.Tags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray()
                        ?? [],
                });
            }
        }

        var specialCoefficients = new List<UnitSpecialCoefficientDefinition>();
        if (document.SpecialCoefficients is not null)
        {
            foreach (var entry in document.SpecialCoefficients)
            {
                if (entry.When is null)
                    throw new UnitLoadException($"Unit '{contentId.Full}' specialCoefficients entry needs when.");

                specialCoefficients.Add(new UnitSpecialCoefficientDefinition
                {
                    When = new UnitSpecialWhenDefinition
                    {
                        IsDefault = entry.When.Default == true,
                        TargetHasTag = string.IsNullOrWhiteSpace(entry.When.TargetHasTag)
                            ? null
                            : entry.When.TargetHasTag,
                        ManhattanRange = entry.When.ManhattanRange,
                    },
                    Multiply = entry.Multiply,
                });
            }
        }

        if (specialCoefficients.Count == 0)
        {
            specialCoefficients.Add(new UnitSpecialCoefficientDefinition
            {
                When = new UnitSpecialWhenDefinition { IsDefault = true },
                Multiply = 1.0,
            });
        }

        return new UnitDefinition
        {
            ContentId = contentId,
            DisplayNameKey = string.IsNullOrWhiteSpace(document.DisplayNameKey)
                ? $"units.{localId}"
                : document.DisplayNameKey,
            MovementClass = MovementClassIds.Parse(document.MovementClass),
            Tags = document.Tags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray() ?? [],
            Recruitable = document.Recruitable ?? true,
            Attack = document.Attack,
            Defence = document.Defence,
            MaxHealth = document.MaxHealth,
            AttackRangeMin = document.AttackRangeMin,
            AttackRangeMax = document.AttackRangeMax,
            Speed = document.Speed,
            Cost = document.Cost,
            Abilities = abilities,
            SpecialCoefficients = specialCoefficients,
            LeavesGravestone = document.LeavesGravestone ?? true,
            Sprites = sprites,
            SourceModuleRootPath = moduleRootPath,
        };
    }
}
