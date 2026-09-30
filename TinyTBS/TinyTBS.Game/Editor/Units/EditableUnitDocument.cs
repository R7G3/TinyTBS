using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Game.Editor.Units;

/// <summary>Mutable Units/{id}.json for the Editor master.</summary>
public sealed class EditableUnitDocument
{
    public string OriginalId { get; set; } = "unit";

    public string Id { get; set; } = "unit";

    public string DisplayNameKey { get; set; } = "units.unit";

    public string MovementClass { get; set; } = MovementClassIds.Foot;

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
            MovementClass = MovementClassIds.Foot,
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

    public static EditableUnitDocument Load(string unitJsonPath, IFileSystem files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitJsonPath);
        ArgumentNullException.ThrowIfNull(files);

        if (!files.Exists(unitJsonPath))
            throw new EditorException("Missing unit file: " + unitJsonPath);

        var moduleRoot = EditorModuleRoot.Find(files, unitJsonPath);
        var contentNamespace = ReadContentNamespace(files, moduleRoot);
        using var stream = files.OpenRead(unitJsonPath);
        UnitDefinition definition;
        try
        {
            definition = UnitJsonParser.ParseUnit(stream, contentNamespace, moduleRoot);
        }
        catch (UnitLoadException exception)
        {
            throw new EditorException(exception.Message, exception);
        }

        var id = definition.ContentId.LocalId;
        return new EditableUnitDocument
        {
            OriginalId = id,
            Id = id,
            DisplayNameKey = definition.DisplayNameKey,
            MovementClass = MovementClassIds.ToId(definition.MovementClass),
            Tags = definition.Tags.ToList(),
            Recruitable = definition.Recruitable,
            Attack = definition.Attack,
            Defence = definition.Defence,
            MaxHealth = definition.MaxHealth,
            AttackRangeMin = definition.AttackRangeMin,
            AttackRangeMax = definition.AttackRangeMax,
            Speed = definition.Speed,
            Cost = definition.Cost,
            Abilities = definition.Abilities
                .Select(ability => new EditableUnitAbility
                {
                    Type = ability.Type,
                    Amount = ability.Amount,
                    MinRange = ability.MinRange,
                    Value = ability.Value,
                    Radius = ability.Radius,
                    Tags = ability.Tags.ToList(),
                })
                .ToList(),
            SpecialCoefficients = definition.SpecialCoefficients
                .Select(coefficient => new EditableUnitSpecialCoefficient
                {
                    WhenDefault = coefficient.When.IsDefault,
                    TargetHasTag = coefficient.When.TargetHasTag,
                    ManhattanRange = coefficient.When.ManhattanRange,
                    Multiply = coefficient.Multiply,
                })
                .ToList(),
            LeavesGravestone = definition.LeavesGravestone,
            SpriteBase = NormalizePath(definition.Sprites?.BasePath),
            SpriteMask = NormalizePath(definition.Sprites?.MaskPath),
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
            return UnitJsonParser.ParseModuleManifest(stream).ContentNamespace;
        }
        catch (UnitLoadException exception)
        {
            GameLog.Warning(
                $"Units module manifest '{manifestPath}' could not be read; using the folder name as the namespace.",
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
