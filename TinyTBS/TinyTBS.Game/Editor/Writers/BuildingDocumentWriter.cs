using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes <c>Buildings/{id}.json</c> under a buildings module.</summary>
public sealed class BuildingDocumentWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileContentProvider _files;

    public BuildingDocumentWriter(IFileContentProvider files)
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
        Directory.CreateDirectory(buildingsDir);

        BuildingHealWriteDto? heal = null;
        if (document.Heal is not null)
        {
            heal = new BuildingHealWriteDto
            {
                Amount = Math.Max(0, document.Heal.Amount),
                Scope = string.IsNullOrWhiteSpace(document.Heal.Scope) ? "allied" : document.Heal.Scope.Trim(),
            };
        }

        BuildingRuinedWriteDto? ruined = null;
        if (document.HasRuined)
        {
            var source = document.Ruined ?? new EditableBuildingRuined();
            ruined = new BuildingRuinedWriteDto
            {
                Income = source.Income,
                DefenceBonus = source.DefenceBonus,
                Capturable = source.Capturable,
                Heal = source.Heal is null
                    ? null
                    : new BuildingHealWriteDto
                    {
                        Amount = Math.Max(0, source.Heal.Amount),
                        Scope = string.IsNullOrWhiteSpace(source.Heal.Scope) ? "none" : source.Heal.Scope.Trim(),
                    },
            };
        }

        var payload = new BuildingWriteDto
        {
            FormatVersion = 1,
            Id = id,
            DisplayNameKey = string.IsNullOrWhiteSpace(document.DisplayNameKey)
                ? "buildings." + id
                : document.DisplayNameKey.Trim(),
            Tags = document.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList(),
            Sprites = new BuildingSpritesWriteDto
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
        File.WriteAllText(path, JsonSerializer.Serialize(payload, WriteOptions) + Environment.NewLine, Encoding.UTF8);

        var originalId = string.IsNullOrWhiteSpace(document.OriginalId) ? id : document.OriginalId.Trim();
        if (!string.Equals(originalId, id, StringComparison.Ordinal))
        {
            var oldPath = _files.Combine(buildingsDir, originalId + ".json");
            if (File.Exists(oldPath) && !string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
                File.Delete(oldPath);
        }

        document.OriginalId = id;
        document.IsDirty = false;
        return path;
    }

    private string ResolveBuildingsDir(string moduleRoot)
    {
        var moduleJsonPath = _files.Combine(moduleRoot, ContentModuleFiles.ModuleJsonFileName);
        if (!File.Exists(moduleJsonPath))
            return _files.Combine(moduleRoot, "Buildings");

        try
        {
            using var stream = File.OpenRead(moduleJsonPath);
            var manifest = JsonSerializer.Deserialize<BuildingModuleManifestReadDto>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            var relative = manifest?.Content?.BuildingsDir;
            if (!string.IsNullOrWhiteSpace(relative))
                return _files.Combine(moduleRoot, relative.Trim().Replace('/', Path.DirectorySeparatorChar));
        }
        catch (JsonException)
        {
        }

        return _files.Combine(moduleRoot, "Buildings");
    }

    private sealed class BuildingModuleManifestReadDto
    {
        [JsonPropertyName("content")]
        public BuildingModuleContentReadDto? Content { get; set; }
    }

    private sealed class BuildingModuleContentReadDto
    {
        [JsonPropertyName("buildingsDir")]
        public string? BuildingsDir { get; set; }
    }

    private sealed class BuildingWriteDto
    {
        [JsonPropertyName("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("displayNameKey")]
        public string DisplayNameKey { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = [];

        [JsonPropertyName("sprites")]
        public BuildingSpritesWriteDto Sprites { get; set; } = new();

        [JsonPropertyName("income")]
        public int Income { get; set; }

        [JsonPropertyName("defenceBonus")]
        public int DefenceBonus { get; set; }

        [JsonPropertyName("allowsRecruit")]
        public bool AllowsRecruit { get; set; }

        [JsonPropertyName("recruitFromTags")]
        public List<string> RecruitFromTags { get; set; } = [];

        [JsonPropertyName("heal")]
        public BuildingHealWriteDto? Heal { get; set; }

        [JsonPropertyName("capturable")]
        public bool Capturable { get; set; }

        [JsonPropertyName("destroyable")]
        public bool Destroyable { get; set; }

        [JsonPropertyName("repairable")]
        public bool Repairable { get; set; }

        [JsonPropertyName("ruined")]
        public BuildingRuinedWriteDto? Ruined { get; set; }

        [JsonPropertyName("countsTowardPlayerDefeat")]
        public bool CountsTowardPlayerDefeat { get; set; }
    }

    private sealed class BuildingSpritesWriteDto
    {
        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;

        [JsonPropertyName("mask")]
        public string Mask { get; set; } = string.Empty;

        [JsonPropertyName("ruinedBase")]
        public string? RuinedBase { get; set; }

        [JsonPropertyName("ruinedMask")]
        public string? RuinedMask { get; set; }
    }

    private sealed class BuildingHealWriteDto
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = "allied";
    }

    private sealed class BuildingRuinedWriteDto
    {
        [JsonPropertyName("income")]
        public int Income { get; set; }

        [JsonPropertyName("defenceBonus")]
        public int DefenceBonus { get; set; }

        [JsonPropertyName("heal")]
        public BuildingHealWriteDto? Heal { get; set; }

        [JsonPropertyName("capturable")]
        public bool Capturable { get; set; }
    }
}
