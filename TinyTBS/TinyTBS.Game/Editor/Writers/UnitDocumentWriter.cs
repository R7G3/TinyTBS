using System.Text;
using System.Text.Json;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Units;
using TinyTBS.Game.Modules;
using TinyTBS.Rules;
using TinyTBS.Rules.Units.Models;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes <c>Units/{id}.json</c> and syncs <c>recruit.addsToPool</c> in module.json.</summary>
public sealed class UnitDocumentWriter
{
    private readonly IFileSystem _files;

    public UnitDocumentWriter(IFileSystem files)
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
        _files.CreateDirectory(unitsDir);

        var payload = new UnitDefinitionDto
        {
            FormatVersion = 1,
            Id = id,
            DisplayNameKey = string.IsNullOrWhiteSpace(document.DisplayNameKey)
                ? "units." + id
                : document.DisplayNameKey.Trim(),
            MovementClass = MovementClassIds.Normalize(document.MovementClass),
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
        _files.WriteAllText(path, JsonSerializer.Serialize(payload, ContentJson.Write) + Environment.NewLine, Encoding.UTF8);

        var originalId = string.IsNullOrWhiteSpace(document.OriginalId) ? id : document.OriginalId.Trim();
        if (!string.Equals(originalId, id, StringComparison.Ordinal))
        {
            var oldPath = _files.Combine(unitsDir, originalId + ".json");
            if (_files.Exists(oldPath) && !string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
                _files.DeleteFile(oldPath);
        }

        SyncRecruitPool(unitsModuleRoot, originalId, id, document.Recruitable);
        document.OriginalId = id;
        document.IsDirty = false;
        return path;
    }

    private string ResolveUnitsDir(string moduleRoot)
    {
        var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!_files.Exists(moduleJsonPath))
            return _files.Combine(moduleRoot, "Units");

        try
        {
            using var stream = _files.OpenRead(moduleJsonPath);
            var unitsDir = UnitJsonParser.ParseModuleManifest(stream).UnitsDir;
            if (!string.IsNullOrWhiteSpace(unitsDir))
                return _files.Combine(moduleRoot, unitsDir.Trim().Replace('/', Path.DirectorySeparatorChar));
        }
        catch (UnitLoadException exception)
        {
            GameLog.Warning(
                $"Units module manifest '{moduleJsonPath}' could not be read; using the default Units folder.",
                exception);
        }

        return _files.Combine(moduleRoot, "Units");
    }

    private string ResolveContentNamespace(string moduleRoot)
    {
        var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!_files.Exists(moduleJsonPath))
            return Path.GetFileName(moduleRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        try
        {
            using var stream = _files.OpenRead(moduleJsonPath);
            return UnitJsonParser.ParseModuleManifest(stream).ContentNamespace;
        }
        catch (UnitLoadException exception)
        {
            GameLog.Warning(
                $"Units module manifest '{moduleJsonPath}' could not be read; using the folder name as the namespace.",
                exception);
        }

        return Path.GetFileName(moduleRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private void SyncRecruitPool(string moduleRoot, string previousLocalId, string newLocalId, bool recruitable)
    {
        var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!_files.Exists(moduleJsonPath))
            return;

        var contentNamespace = ResolveContentNamespace(moduleRoot);
        var previousFull = contentNamespace + "/" + previousLocalId;
        var newFull = contentNamespace + "/" + newLocalId;

        System.Text.Json.Nodes.JsonNode? root;
        try
        {
            root = System.Text.Json.Nodes.JsonNode.Parse(_files.ReadAllText(moduleJsonPath));
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
        _files.WriteAllText(
            moduleJsonPath,
            rootObject.ToJsonString(ContentJson.Write) + Environment.NewLine,
            Encoding.UTF8);
    }
}
