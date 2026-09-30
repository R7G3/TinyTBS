using System.Text;
using System.Text.Json;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Rules;
using TinyTBS.Rules.Buildings.Models;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes <c>Buildings/{id}.json</c> under a buildings module.</summary>
public sealed class BuildingDocumentWriter
{
    private readonly IFileSystem _files;

    public BuildingDocumentWriter(IFileSystem files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public string Write(string buildingsModuleRoot, EditableBuildingDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buildingsModuleRoot);
        ArgumentNullException.ThrowIfNull(document);

        ContentModuleManifestParser.ValidateModuleId(document.Id);
        var id = document.Id.Trim();
        var buildingsDir = ResolveBuildingsDir(buildingsModuleRoot);
        _files.CreateDirectory(buildingsDir);

        BuildingHealDto? heal = null;
        if (document.Heal is not null)
        {
            heal = new BuildingHealDto
            {
                Amount = Math.Max(0, document.Heal.Amount),
                Scope = string.IsNullOrWhiteSpace(document.Heal.Scope) ? "allied" : document.Heal.Scope.Trim(),
            };
        }

        BuildingRuinedDto? ruined = null;
        if (document.HasRuined)
        {
            var source = document.Ruined ?? new EditableBuildingRuined();
            ruined = new BuildingRuinedDto
            {
                Income = source.Income,
                DefenceBonus = source.DefenceBonus,
                Capturable = source.Capturable,
                Heal = source.Heal is null
                    ? null
                    : new BuildingHealDto
                    {
                        Amount = Math.Max(0, source.Heal.Amount),
                        Scope = string.IsNullOrWhiteSpace(source.Heal.Scope) ? "none" : source.Heal.Scope.Trim(),
                    },
            };
        }

        var payload = new BuildingDefinitionDto
        {
            FormatVersion = 1,
            Id = id,
            DisplayNameKey = string.IsNullOrWhiteSpace(document.DisplayNameKey)
                ? "buildings." + id
                : document.DisplayNameKey.Trim(),
            Tags = document.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList(),
            Sprites = new BuildingSpritesDto
            {
                Base = document.SpriteBase.Trim().Replace('\\', '/'),
                Mask = document.SpriteMask.Trim().Replace('\\', '/'),
                RuinedBase = string.IsNullOrWhiteSpace(document.SpriteRuinedBase)
                    ? null
                    : document.SpriteRuinedBase.Trim().Replace('\\', '/'),
                RuinedMask = string.IsNullOrWhiteSpace(document.SpriteRuinedMask)
                    ? null
                    : document.SpriteRuinedMask.Trim().Replace('\\', '/'),
            },
            Income = document.Income,
            DefenceBonus = document.DefenceBonus,
            AllowsRecruit = document.AllowsRecruit,
            RecruitFromTags = document.RecruitFromTags
                .Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList(),
            Heal = heal,
            Capturable = document.Capturable,
            Destroyable = document.Destroyable,
            Repairable = document.Repairable,
            Ruined = ruined,
            CountsTowardPlayerDefeat = document.CountsTowardPlayerDefeat,
        };

        var path = _files.Combine(buildingsDir, id + ".json");
        _files.WriteAllText(path, JsonSerializer.Serialize(payload, ContentJson.Write) + Environment.NewLine, Encoding.UTF8);

        var originalId = string.IsNullOrWhiteSpace(document.OriginalId) ? id : document.OriginalId.Trim();
        if (!string.Equals(originalId, id, StringComparison.Ordinal))
        {
            var oldPath = _files.Combine(buildingsDir, originalId + ".json");
            if (_files.Exists(oldPath) && !string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
                _files.DeleteFile(oldPath);
        }

        document.OriginalId = id;
        document.IsDirty = false;
        return path;
    }

    private string ResolveBuildingsDir(string moduleRoot)
    {
        var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!_files.Exists(moduleJsonPath))
            return _files.Combine(moduleRoot, "Buildings");

        try
        {
            using var stream = _files.OpenRead(moduleJsonPath);
            var buildingsDir = BuildingJsonParser.ParseModuleManifest(stream).BuildingsDir;
            if (!string.IsNullOrWhiteSpace(buildingsDir))
                return _files.Combine(moduleRoot, buildingsDir.Trim().Replace('/', Path.DirectorySeparatorChar));
        }
        catch (BuildingLoadException exception)
        {
            GameLog.Warning(
                $"Buildings module manifest '{moduleJsonPath}' could not be read; using the default Buildings folder.",
                exception);
        }

        return _files.Combine(moduleRoot, "Buildings");
    }
}
