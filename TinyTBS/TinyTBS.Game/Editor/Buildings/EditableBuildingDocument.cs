using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Buildings.Models;

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

    public EditableBuildingHeal? Heal { get; set; } = new() { Amount = 20, Scope = BuildingHealScopeIds.Allied };

    public bool Capturable { get; set; } = true;

    public bool Destroyable { get; set; }

    public bool Repairable { get; set; }

    public bool HasRuined { get; set; }

    public EditableBuildingRuined? Ruined { get; set; }

    public bool CountsTowardPlayerDefeat { get; set; }

    public bool IsDirty { get; set; }

    public static EditableBuildingDocument CreateDefault(string buildingId)
    {
        var id = string.IsNullOrWhiteSpace(buildingId) ? "building" : buildingId;
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
            Heal = new EditableBuildingHeal { Amount = 20, Scope = BuildingHealScopeIds.Allied },
            Capturable = true,
            Destroyable = false,
            Repairable = false,
            HasRuined = false,
            Ruined = null,
            CountsTowardPlayerDefeat = false,
            IsDirty = true,
        };
    }

    public static EditableBuildingDocument Load(string buildingJsonPath, IFileSystem files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buildingJsonPath);
        ArgumentNullException.ThrowIfNull(files);

        if (!files.Exists(buildingJsonPath))
            throw new EditorException("Missing building file: " + buildingJsonPath);

        var moduleRoot = EditorModuleRoot.Find(files, buildingJsonPath);
        var contentNamespace = ReadContentNamespace(files, moduleRoot);
        using var stream = files.OpenRead(buildingJsonPath);
        BuildingDefinition definition;
        try
        {
            definition = BuildingJsonParser.ParseBuilding(stream, contentNamespace, moduleRoot);
        }
        catch (BuildingLoadException exception)
        {
            throw new EditorException(exception.Message, exception);
        }

        var id = definition.ContentId.LocalId;
        return new EditableBuildingDocument
        {
            OriginalId = id,
            Id = id,
            DisplayNameKey = definition.DisplayNameKey,
            Tags = definition.Tags.ToList(),
            SpriteBase = definition.Sprites.BasePath.Replace('\\', '/'),
            SpriteMask = definition.Sprites.MaskPath.Replace('\\', '/'),
            SpriteRuinedBase = NormalizePath(definition.Sprites.RuinedBasePath),
            SpriteRuinedMask = NormalizePath(definition.Sprites.RuinedMaskPath),
            Income = definition.Income,
            DefenceBonus = definition.DefenceBonus,
            AllowsRecruit = definition.AllowsRecruit,
            RecruitFromTags = definition.RecruitFromTags.ToList(),
            Heal = definition.Heal is null
                ? null
                : new EditableBuildingHeal
                {
                    Amount = definition.Heal.Amount,
                    Scope = definition.Heal.Scope,
                },
            Capturable = definition.Capturable,
            Destroyable = definition.Destroyable,
            Repairable = definition.Repairable,
            HasRuined = definition.Ruined is not null,
            Ruined = definition.Ruined is null
                ? null
                : new EditableBuildingRuined
                {
                    Income = definition.Ruined.Income,
                    DefenceBonus = definition.Ruined.DefenceBonus,
                    Capturable = definition.Ruined.Capturable,
                    Heal = definition.Ruined.Heal is null
                        ? null
                        : new EditableBuildingHeal
                        {
                            Amount = definition.Ruined.Heal.Amount,
                            Scope = definition.Ruined.Heal.Scope,
                        },
                },
            CountsTowardPlayerDefeat = definition.CountsTowardPlayerDefeat,
            IsDirty = false,
        };
    }

    private static string ReadContentNamespace(IFileSystem files, string moduleRoot)
    {
        var manifestPath = files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!files.Exists(manifestPath))
            return "local";

        using var stream = files.OpenRead(manifestPath);
        try
        {
            return BuildingJsonParser.ParseModuleManifest(stream).ContentNamespace;
        }
        catch (BuildingLoadException exception)
        {
            GameLog.Warning(
                $"Buildings module manifest '{manifestPath}' could not be read; using the folder name as the namespace.",
                exception);
            return FolderName(moduleRoot);
        }
    }

    private static string FolderName(string moduleRoot)
    {
        var name = Path.GetFileName(moduleRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(name) ? "local" : name;
    }

    private static string? NormalizePath(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : path.Replace('\\', '/');
}
