using System.Text;
using System.Text.Json;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Modules;
using TinyTBS.Rules;
using TinyTBS.Rules.Buildings.Models;

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
        var id = document.Id;
        document.DisplayNameKey = SavedUserText.Or(document.DisplayNameKey, "buildings." + id);
        document.Tags = SavedUserText.List(document.Tags);
        document.RecruitFromTags = SavedUserText.List(document.RecruitFromTags);
        document.SpriteBase = SavedUserText.Trimmed(document.SpriteBase).Replace('\\', '/');
        document.SpriteMask = SavedUserText.Trimmed(document.SpriteMask).Replace('\\', '/');
        document.SpriteRuinedBase = SavedUserText.Optional(document.SpriteRuinedBase)?.Replace('\\', '/');
        document.SpriteRuinedMask = SavedUserText.Optional(document.SpriteRuinedMask)?.Replace('\\', '/');
        if (document.Heal is not null)
            document.Heal.Scope = SavedUserText.Or(document.Heal.Scope, BuildingHealScopeIds.Allied);
        if (document.Ruined?.Heal is not null)
            document.Ruined.Heal.Scope = SavedUserText.Or(document.Ruined.Heal.Scope, BuildingHealScopeIds.None);

        var buildingsDir = ResolveBuildingsDir(buildingsModuleRoot);
        _files.CreateDirectory(buildingsDir);

        BuildingHealDto? heal = null;
        if (document.Heal is not null)
        {
            heal = new BuildingHealDto
            {
                Amount = Math.Max(0, document.Heal.Amount),
                Scope = document.Heal.Scope,
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
                        Scope = source.Heal.Scope,
                    },
            };
        }

        var payload = new BuildingDefinitionDto
        {
            FormatVersion = 1,
            Id = id,
            DisplayNameKey = document.DisplayNameKey,
            Tags = document.Tags,
            Sprites = new BuildingSpritesDto
            {
                Base = document.SpriteBase,
                Mask = document.SpriteMask,
                RuinedBase = document.SpriteRuinedBase,
                RuinedMask = document.SpriteRuinedMask,
            },
            Income = document.Income,
            DefenceBonus = document.DefenceBonus,
            AllowsRecruit = document.AllowsRecruit,
            RecruitFromTags = document.RecruitFromTags,
            Heal = heal,
            Capturable = document.Capturable,
            Destroyable = document.Destroyable,
            Repairable = document.Repairable,
            Ruined = ruined,
            CountsTowardPlayerDefeat = document.CountsTowardPlayerDefeat,
        };

        var path = _files.Combine(buildingsDir, id + ".json");
        _files.WriteAllText(path, JsonSerializer.Serialize(payload, ContentJson.Write) + Environment.NewLine, Encoding.UTF8);

        var originalId = string.IsNullOrWhiteSpace(document.OriginalId) ? id : document.OriginalId;
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
                return _files.Combine(moduleRoot, buildingsDir.Replace('/', Path.DirectorySeparatorChar));
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
