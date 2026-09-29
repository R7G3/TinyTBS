using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Editor.Units;

/// <summary>Mutable Units/{id}.json for the Editor master.</summary>
public sealed class EditableUnitDocument
{
    public string OriginalId { get; set; } = "unit";

    public string Id { get; set; } = "unit";

    public string DisplayNameKey { get; set; } = "units.unit";

    public string MovementClass { get; set; } = "foot";

    public List<string> Tags { get; set; } = [];

    public bool Recruitable { get; set; } = true;

    public int Attack { get; set; } = 40;

    public int Defence { get; set; } = 5;

    public int MaxHealth { get; set; } = 100;

    public int AttackRangeMin { get; set; } = 1;

    public int AttackRangeMax { get; set; } = 1;

    public int Speed { get; set; } = 4;

    public int Cost { get; set; } = 100;

    public List<EditableUnitAbility> Abilities { get; set; } = [];

    public List<EditableUnitSpecialCoefficient> SpecialCoefficients { get; set; } = [];

    public bool LeavesGravestone { get; set; } = true;

    public string? SpriteBase { get; set; }

    public string? SpriteMask { get; set; }

    public bool IsDirty { get; set; }

    public static EditableUnitDocument CreateDefault(string unitId)
    {
        var id = string.IsNullOrWhiteSpace(unitId) ? "unit" : unitId.Trim();
        return new EditableUnitDocument
        {
            OriginalId = id,
            Id = id,
            DisplayNameKey = "units." + id,
            MovementClass = "foot",
            Tags = [],
            Recruitable = true,
            Attack = 40,
            Defence = 5,
            MaxHealth = 100,
            AttackRangeMin = 1,
            AttackRangeMax = 1,
            Speed = 4,
            Cost = 100,
            Abilities = [],
            SpecialCoefficients =
            [
                new EditableUnitSpecialCoefficient { WhenDefault = true, Multiply = 1.0 },
            ],
            LeavesGravestone = true,
            SpriteBase = "Resources/Images/units/" + id + "_base.png",
            SpriteMask = "Resources/Images/units/" + id + "_mask.png",
            IsDirty = true,
        };
    }

    public static EditableUnitDocument Load(string unitJsonPath, IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitJsonPath);
        ArgumentNullException.ThrowIfNull(files);

        if (!files.Exists(unitJsonPath))
            throw new EditorException("Missing unit file: " + unitJsonPath);

        using var stream = files.OpenRead(unitJsonPath);
        UnitDefinitionDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<UnitDefinitionDto>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        }
        catch (JsonException exception)
        {
            throw new EditorException("Failed to parse unit JSON.", exception);
        }

        if (dto is null)
            throw new EditorException("Unit JSON deserialized to null.");

        var id = string.IsNullOrWhiteSpace(dto.Id) ? "unit" : dto.Id.Trim();
        return new EditableUnitDocument
        {
            OriginalId = id,
            Id = id,
            DisplayNameKey = string.IsNullOrWhiteSpace(dto.DisplayNameKey) ? "units." + id : dto.DisplayNameKey.Trim(),
            MovementClass = string.IsNullOrWhiteSpace(dto.MovementClass) ? "foot" : dto.MovementClass.Trim(),
            Tags = dto.Tags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList() ?? [],
            Recruitable = dto.Recruitable ?? true,
            Attack = dto.Attack,
            Defence = dto.Defence,
            MaxHealth = Math.Max(1, dto.MaxHealth),
            AttackRangeMin = Math.Max(0, dto.AttackRangeMin),
            AttackRangeMax = Math.Max(dto.AttackRangeMin, dto.AttackRangeMax),
            Speed = Math.Max(0, dto.Speed),
            Cost = Math.Max(0, dto.Cost),
            Abilities = (dto.Abilities ?? [])
                .Select(ability => new EditableUnitAbility
                {
                    Type = string.IsNullOrWhiteSpace(ability.Type) ? "captureBuilding" : ability.Type.Trim(),
                    Amount = ability.Amount,
                    MinRange = ability.MinRange,
                    Value = ability.Value,
                    Radius = ability.Radius,
                    Tags = ability.Tags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList()
                        ?? [],
                })
                .ToList(),
            SpecialCoefficients = (dto.SpecialCoefficients ?? [])
                .Select(coefficient => new EditableUnitSpecialCoefficient
                {
                    WhenDefault = coefficient.When?.Default == true,
                    TargetHasTag = string.IsNullOrWhiteSpace(coefficient.When?.TargetHasTag)
                        ? null
                        : coefficient.When!.TargetHasTag!.Trim(),
                    ManhattanRange = coefficient.When?.ManhattanRange,
                    Multiply = coefficient.Multiply,
                })
                .ToList(),
            LeavesGravestone = dto.LeavesGravestone ?? true,
            SpriteBase = string.IsNullOrWhiteSpace(dto.Sprites?.Base) ? null : dto.Sprites!.Base!.Trim().Replace('\\', '/'),
            SpriteMask = string.IsNullOrWhiteSpace(dto.Sprites?.Mask) ? null : dto.Sprites!.Mask!.Trim().Replace('\\', '/'),
            IsDirty = false,
        };
    }
}
