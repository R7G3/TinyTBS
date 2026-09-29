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
        BuildingDefinitionDto? definitionDto;
        try
        {
            definitionDto = JsonSerializer.Deserialize<BuildingDefinitionDto>(stream, new JsonSerializerOptions
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

        if (definitionDto is null)
            throw new EditorException("Building JSON deserialized to null.");

        var id = string.IsNullOrWhiteSpace(definitionDto.Id) ? "building" : definitionDto.Id.Trim();
        EditableBuildingHeal? heal = null;
        if (definitionDto.Heal is not null)
        {
            heal = new EditableBuildingHeal
            {
                Amount = definitionDto.Heal.Amount,
                Scope = string.IsNullOrWhiteSpace(definitionDto.Heal.Scope) ? "allied" : definitionDto.Heal.Scope.Trim(),
            };
        }

        EditableBuildingRuined? ruined = null;
        var hasRuined = definitionDto.Ruined is not null;
        if (definitionDto.Ruined is not null)
        {
            ruined = new EditableBuildingRuined
            {
                Income = definitionDto.Ruined.Income,
                DefenceBonus = definitionDto.Ruined.DefenceBonus,
                Capturable = definitionDto.Ruined.Capturable,
                Heal = definitionDto.Ruined.Heal is null
                    ? null
                    : new EditableBuildingHeal
                    {
                        Amount = definitionDto.Ruined.Heal.Amount,
                        Scope = string.IsNullOrWhiteSpace(definitionDto.Ruined.Heal.Scope)
                            ? "none"
                            : definitionDto.Ruined.Heal.Scope.Trim(),
                    },
            };
        }

        return new EditableBuildingDocument
        {
            OriginalId = id,
            Id = id,
            DisplayNameKey = string.IsNullOrWhiteSpace(definitionDto.DisplayNameKey)
                ? "buildings." + id
                : definitionDto.DisplayNameKey.Trim(),
            Tags = definitionDto.Tags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList() ?? [],
            SpriteBase = string.IsNullOrWhiteSpace(definitionDto.Sprites?.Base)
                ? "Resources/Images/buildings/" + id + "_base.png"
                : definitionDto.Sprites!.Base!.Trim().Replace('\\', '/'),
            SpriteMask = string.IsNullOrWhiteSpace(definitionDto.Sprites?.Mask)
                ? "Resources/Images/buildings/" + id + "_mask.png"
                : definitionDto.Sprites!.Mask!.Trim().Replace('\\', '/'),
            SpriteRuinedBase = string.IsNullOrWhiteSpace(definitionDto.Sprites?.RuinedBase)
                ? null
                : definitionDto.Sprites!.RuinedBase!.Trim().Replace('\\', '/'),
            SpriteRuinedMask = string.IsNullOrWhiteSpace(definitionDto.Sprites?.RuinedMask)
                ? null
                : definitionDto.Sprites!.RuinedMask!.Trim().Replace('\\', '/'),
            Income = definitionDto.Income,
            DefenceBonus = definitionDto.DefenceBonus,
            AllowsRecruit = definitionDto.AllowsRecruit,
            RecruitFromTags = definitionDto.RecruitFromTags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList() ?? [],
            Heal = heal,
            Capturable = definitionDto.Capturable,
            Destroyable = definitionDto.Destroyable,
            Repairable = definitionDto.Repairable,
            HasRuined = hasRuined,
            Ruined = ruined,
            CountsTowardPlayerDefeat = definitionDto.CountsTowardPlayerDefeat,
            IsDirty = false,
        };
    }
}
