using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Units;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes <c>Units/{id}.json</c> and syncs <c>recruit.addsToPool</c> in module.json.</summary>
public sealed class UnitDocumentWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileContentProvider _files;

    public UnitDocumentWriter(IFileContentProvider files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string Write(string unitsModuleRoot, EditableUnitDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitsModuleRoot);
        ArgumentNullException.ThrowIfNull(document);

        ContentModuleManifestParser.ValidateModuleId(document.Id);
        var id = document.Id.Trim();
        var unitsDir = ResolveUnitsDir(unitsModuleRoot);
        Directory.CreateDirectory(unitsDir);

        var payload = new UnitDefinitionDto
        {
            FormatVersion = 1,
            Id = id,
            DisplayNameKey = string.IsNullOrWhiteSpace(document.DisplayNameKey)
                ? "units." + id
                : document.DisplayNameKey.Trim(),
            MovementClass = NormalizeMovement(document.MovementClass),
            Tags = document.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList(),
            Recruitable = document.Recruitable,
            Attack = document.Attack,
            Defence = document.Defence,
            MaxHealth = Math.Max(1, document.MaxHealth),
            AttackRangeMin = Math.Max(0, document.AttackRangeMin),
            AttackRangeMax = Math.Max(document.AttackRangeMin, document.AttackRangeMax),
            Speed = Math.Max(0, document.Speed),
            Cost = Math.Max(0, document.Cost),
            Abilities = document.Abilities.Select(ability => new UnitAbilityDto
            {
                Type = ability.Type.Trim(),
                Amount = ability.Amount,
                MinRange = ability.MinRange,
                Value = ability.Value,
                Radius = ability.Radius,
                Tags = ability.Tags.Count == 0
                    ? null
                    : ability.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList(),
            }).ToList(),
            SpecialCoefficients = document.SpecialCoefficients.Select(coefficient => new UnitSpecialCoefficientDto
            {
                When = new UnitSpecialWhenDto
                {
                    Default = coefficient.WhenDefault ? true : null,
                    TargetHasTag = coefficient.WhenDefault || string.IsNullOrWhiteSpace(coefficient.TargetHasTag)
                        ? null
                        : coefficient.TargetHasTag.Trim(),
                    ManhattanRange = coefficient.WhenDefault || !string.IsNullOrWhiteSpace(coefficient.TargetHasTag)
                        ? null
                        : coefficient.ManhattanRange,
                },
                Multiply = coefficient.Multiply,
            }).ToList(),
            LeavesGravestone = document.LeavesGravestone,
            Sprites = new UnitSpritesDto
            {
                Base = string.IsNullOrWhiteSpace(document.SpriteBase) ? null : document.SpriteBase.Trim().Replace('\\', '/'),
                Mask = string.IsNullOrWhiteSpace(document.SpriteMask) ? null : document.SpriteMask.Trim().Replace('\\', '/'),
            },
        };

        var path = _files.Combine(unitsDir, id + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(payload, WriteOptions) + Environment.NewLine, Encoding.UTF8);

        var originalId = string.IsNullOrWhiteSpace(document.OriginalId) ? id : document.OriginalId.Trim();
        if (!string.Equals(originalId, id, StringComparison.Ordinal))
        {
            var oldPath = _files.Combine(unitsDir, originalId + ".json");
            if (File.Exists(oldPath) && !string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
                File.Delete(oldPath);
        }

        SyncRecruitPool(unitsModuleRoot, originalId, id, document.Recruitable);
        document.OriginalId = id;
        document.IsDirty = false;
        return path;
    }

    private string ResolveUnitsDir(string moduleRoot)
    {
        var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!File.Exists(moduleJsonPath))
            return _files.Combine(moduleRoot, "Units");

        try
        {
            using var stream = File.OpenRead(moduleJsonPath);
            var manifest = JsonSerializer.Deserialize<UnitModuleJsonDto>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            var relative = manifest?.Content?.UnitsDir;
            if (!string.IsNullOrWhiteSpace(relative))
                return _files.Combine(moduleRoot, relative.Trim().Replace('/', Path.DirectorySeparatorChar));
        }
        catch (JsonException)
        {
        }

        return _files.Combine(moduleRoot, "Units");
    }

    private string ResolveContentNamespace(string moduleRoot)
    {
        var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!File.Exists(moduleJsonPath))
            return Path.GetFileName(moduleRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        try
        {
            using var stream = File.OpenRead(moduleJsonPath);
            var manifest = JsonSerializer.Deserialize<UnitModuleJsonDto>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            if (!string.IsNullOrWhiteSpace(manifest?.Namespace))
                return manifest.Namespace.Trim();
            if (!string.IsNullOrWhiteSpace(manifest?.Id))
                return manifest.Id.Trim();
        }
        catch (JsonException)
        {
        }

        return Path.GetFileName(moduleRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private void SyncRecruitPool(string moduleRoot, string previousLocalId, string newLocalId, bool recruitable)
    {
        var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!File.Exists(moduleJsonPath))
            return;

        var contentNamespace = ResolveContentNamespace(moduleRoot);
        var previousFull = contentNamespace + "/" + previousLocalId;
        var newFull = contentNamespace + "/" + newLocalId;

        System.Text.Json.Nodes.JsonNode? root;
        try
        {
            root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(moduleJsonPath));
        }
        catch (JsonException)
        {
            return;
        }

        if (root is not System.Text.Json.Nodes.JsonObject rootObject)
            return;

        if (rootObject["recruit"] is not System.Text.Json.Nodes.JsonObject recruitObject)
        {
            recruitObject = new System.Text.Json.Nodes.JsonObject();
            rootObject["recruit"] = recruitObject;
        }

        var pool = new System.Text.Json.Nodes.JsonArray();
        if (recruitObject["addsToPool"] is System.Text.Json.Nodes.JsonArray existing)
        {
            foreach (var entry in existing)
            {
                var value = entry?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                if (string.Equals(value, previousFull, StringComparison.Ordinal)
                    || string.Equals(value, newFull, StringComparison.Ordinal)
                    || string.Equals(value, previousLocalId, StringComparison.Ordinal)
                    || string.Equals(value, newLocalId, StringComparison.Ordinal))
                {
                    continue;
                }

                pool.Add(value.Trim());
            }
        }

        if (recruitable)
            pool.Add(newFull);

        recruitObject["addsToPool"] = pool;
        File.WriteAllText(
            moduleJsonPath,
            rootObject.ToJsonString(WriteOptions) + Environment.NewLine,
            Encoding.UTF8);
    }

    private static string NormalizeMovement(string movementClass)
    {
        var value = string.IsNullOrWhiteSpace(movementClass) ? "foot" : movementClass.Trim().ToLowerInvariant();
        return value is "foot" or "water" or "fly" ? value : "foot";
    }
}
