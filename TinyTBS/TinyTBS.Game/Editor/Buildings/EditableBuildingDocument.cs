using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Buildings.Models;

namespace TinyTBS.Game.Editor.Buildings;

/// <summary>Mutable Buildings/{id}.json for the Editor master.</summary>
public sealed class EditableBuildingDocument
{
    public string OriginalId { get; set; } = "building";

    public string Id { get; set; } = "building";

    public string DisplayNameKey { get; set; } = "buildings.building";

    public List<string> Tags { get; set; } = [];

    public string SpriteBase { get; set; } = "Resources/Images/buildings/building_base.png";

    public string SpriteMask { get; set; } = "Resources/Images/buildings/building_mask.png";

    public string? SpriteRuinedBase { get; set; }

    public string? SpriteRuinedMask { get; set; }

    public int Income { get; set; } = 30;

    public int DefenceBonus { get; set; } = 15;

    public bool AllowsRecruit { get; set; }

    public List<string> RecruitFromTags { get; set; } = [];

    public EditableBuildingHeal? Heal { get; set; } = new() { Amount = 20, Scope = "allied" };

    public bool Capturable { get; set; } = true;

    public bool Destroyable { get; set; }

    public bool Repairable { get; set; }

    public bool HasRuined { get; set; }

    public EditableBuildingRuined? Ruined { get; set; }

    public bool CountsTowardPlayerDefeat { get; set; }

    public bool IsDirty { get; set; }

    public static EditableBuildingDocument CreateDefault(string buildingId)
    {
        var id = string.IsNullOrWhiteSpace(buildingId) ? "building" : buildingId.Trim();
        return new EditableBuildingDocument
        {
            OriginalId = id,
            Id = id,
            DisplayNameKey = "buildings." + id,
            Tags = [id],
            SpriteBase = "Resources/Images/buildings/" + id + "_base.png",
            SpriteMask = "Resources/Images/buildings/" + id + "_mask.png",
            Income = 30,
            DefenceBonus = 15,
            AllowsRecruit = false,
            RecruitFromTags = [],
            Heal = new EditableBuildingHeal { Amount = 20, Scope = "allied" },
            Capturable = true,
            Destroyable = false,
            Repairable = false,
            HasRuined = false,
            Ruined = null,
            CountsTowardPlayerDefeat = false,
            IsDirty = true,
        };
    }

    public static EditableBuildingDocument Load(string buildingJsonPath, IFileContentProvider files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buildingJsonPath);
        ArgumentNullException.ThrowIfNull(files);

        if (!files.Exists(buildingJsonPath))
            throw new EditorException("Missing building file: " + buildingJsonPath);

        using var stream = files.OpenRead(buildingJsonPath);
        BuildingDefinitionDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<BuildingDefinitionDto>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
        }
        catch (JsonException exception)
        {
            throw new EditorException("Failed to parse building JSON.", exception);
        }

        if (dto is null)
            throw new EditorException("Building JSON deserialized to null.");

        var id = string.IsNullOrWhiteSpace(dto.Id) ? "building" : dto.Id.Trim();
        EditableBuildingHeal? heal = null;
        if (dto.Heal is not null)
        {
            heal = new EditableBuildingHeal
            {
                Amount = dto.Heal.Amount,
                Scope = string.IsNullOrWhiteSpace(dto.Heal.Scope) ? "allied" : dto.Heal.Scope.Trim(),
            };
        }

        EditableBuildingRuined? ruined = null;
        var hasRuined = dto.Ruined is not null;
        if (dto.Ruined is not null)
        {
            ruined = new EditableBuildingRuined
            {
                Income = dto.Ruined.Income,
                DefenceBonus = dto.Ruined.DefenceBonus,
                Capturable = dto.Ruined.Capturable,
                Heal = dto.Ruined.Heal is null
                    ? null
                    : new EditableBuildingHeal
                    {
                        Amount = dto.Ruined.Heal.Amount,
                        Scope = string.IsNullOrWhiteSpace(dto.Ruined.Heal.Scope)
                            ? "none"
                            : dto.Ruined.Heal.Scope.Trim(),
                    },
            };
        }

        return new EditableBuildingDocument
        {
            OriginalId = id,
            Id = id,
            DisplayNameKey = string.IsNullOrWhiteSpace(dto.DisplayNameKey)
                ? "buildings." + id
                : dto.DisplayNameKey.Trim(),
            Tags = dto.Tags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList() ?? [],
            SpriteBase = string.IsNullOrWhiteSpace(dto.Sprites?.Base)
                ? "Resources/Images/buildings/" + id + "_base.png"
                : dto.Sprites!.Base!.Trim().Replace('\\', '/'),
            SpriteMask = string.IsNullOrWhiteSpace(dto.Sprites?.Mask)
                ? "Resources/Images/buildings/" + id + "_mask.png"
                : dto.Sprites!.Mask!.Trim().Replace('\\', '/'),
            SpriteRuinedBase = string.IsNullOrWhiteSpace(dto.Sprites?.RuinedBase)
                ? null
                : dto.Sprites!.RuinedBase!.Trim().Replace('\\', '/'),
            SpriteRuinedMask = string.IsNullOrWhiteSpace(dto.Sprites?.RuinedMask)
                ? null
                : dto.Sprites!.RuinedMask!.Trim().Replace('\\', '/'),
            Income = dto.Income,
            DefenceBonus = dto.DefenceBonus,
            AllowsRecruit = dto.AllowsRecruit,
            RecruitFromTags = dto.RecruitFromTags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList() ?? [],
            Heal = heal,
            Capturable = dto.Capturable,
            Destroyable = dto.Destroyable,
            Repairable = dto.Repairable,
            HasRuined = hasRuined,
            Ruined = ruined,
            CountsTowardPlayerDefeat = dto.CountsTowardPlayerDefeat,
            IsDirty = false,
        };
    }
}
