using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match;

/// <summary>Loaded textures for the match (terrain/gravestone from theme; unit/building sprites from modules).</summary>
public sealed class MatchTextureAtlas : IDisposable
{
    private readonly List<LoadedTexture> _owned = [];
    private readonly Dictionary<ContentId, TeamSprite> _units;
    private readonly Dictionary<ContentId, TeamSprite> _buildingsIntact;
    private readonly Dictionary<ContentId, TeamSprite> _buildingsRuined;

    private MatchTextureAtlas(
        Texture2D grass,
        Texture2D water,
        Texture2D road,
        Texture2D mountain,
        Texture2D bridge,
        Texture2D forest,
        Texture2D gravestone,
        Dictionary<ContentId, TeamSprite> units,
        Dictionary<ContentId, TeamSprite> buildingsIntact,
        Dictionary<ContentId, TeamSprite> buildingsRuined)
    {
        Grass = grass;
        Water = water;
        Road = road;
        Mountain = mountain;
        Bridge = bridge;
        Forest = forest;
        Gravestone = gravestone;
        _units = units;
        _buildingsIntact = buildingsIntact;
        _buildingsRuined = buildingsRuined;
    }

    public Texture2D Grass { get; }
    public Texture2D Water { get; }
    public Texture2D Road { get; }
    public Texture2D Mountain { get; }
    public Texture2D Bridge { get; }
    public Texture2D Forest { get; }

    /// <summary>Gravestone marker from the theme module.</summary>
    public Texture2D Gravestone { get; }

    public Texture2D Terrain(TerrainKind kind) => kind switch
    {
        TerrainKind.Water => Water,
        TerrainKind.Road => Road,
        TerrainKind.Mountain => Mountain,
        TerrainKind.Bridge => Bridge,
        TerrainKind.Forest => Forest,
        _ => Grass,
    };

    public TeamSprite Unit(ContentId typeId)
    {
        if (_units.TryGetValue(typeId, out var sprite))
            return sprite;

        throw new InvalidOperationException($"No unit sprite loaded for '{typeId.Full}'.");
    }

    public TeamSprite Building(ContentId typeId, bool isRuined = false)
    {
        if (isRuined && _buildingsRuined.TryGetValue(typeId, out var ruined))
            return ruined;

        if (_buildingsIntact.TryGetValue(typeId, out var intact))
            return intact;

        throw new InvalidOperationException($"No building sprite loaded for '{typeId.Full}'.");
    }

    public static MatchTextureAtlas Load(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        IAssetResolver assets,
        MatchContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var ownedTextures = new List<LoadedTexture>();
        var theme = catalog.ThemeModule;

        TeamSprite LoadModulePair(string moduleRoot, string baseRelativePath, string maskRelativePath)
        {
            var baseAbsolute = ModuleSpritePath.CombineModuleFile(moduleRoot, baseRelativePath);
            var maskAbsolute = ModuleSpritePath.CombineModuleFile(moduleRoot, maskRelativePath);
            var baseTexture = GameTextureLoader.LoadModuleSpriteOrFallback(
                graphicsDevice, content, assets, baseAbsolute, baseRelativePath);
            var maskTexture = GameTextureLoader.LoadModuleSpriteOrFallback(
                graphicsDevice, content, assets, maskAbsolute, maskRelativePath);
            ownedTextures.Add(baseTexture);
            ownedTextures.Add(maskTexture);
            return new TeamSprite(baseTexture.Texture, maskTexture.Texture);
        }

        Texture2D LoadThemeTexture(string moduleRelativePath)
        {
            var absolutePath = ModuleSpritePath.CombineModuleFile(theme.ModuleRootPath, moduleRelativePath);
            var loaded = GameTextureLoader.LoadModuleSpriteOrFallback(
                graphicsDevice, content, assets, absolutePath, moduleRelativePath);
            ownedTextures.Add(loaded);
            return loaded.Texture;
        }

        TeamSprite ResolvePair(
            ContentId contentId,
            string owningModuleRoot,
            string baseRelativePath,
            string maskRelativePath)
        {
            if (theme.Remaps.TryGetValue(contentId, out var remap))
            {
                return LoadModulePair(theme.ModuleRootPath, remap.BasePath, remap.MaskPath);
            }

            return LoadModulePair(owningModuleRoot, baseRelativePath, maskRelativePath);
        }

        var units = new Dictionary<ContentId, TeamSprite>();
        foreach (var unitDefinition in catalog.UnitsModule.UnitsById.Values)
        {
            if (unitDefinition.Sprites is null)
            {
                throw new InvalidOperationException(
                    $"Unit '{unitDefinition.ContentId.Full}' has no sprites in the units module.");
            }

            units[unitDefinition.ContentId] = ResolvePair(
                unitDefinition.ContentId,
                unitDefinition.SourceModuleRootPath,
                unitDefinition.Sprites.BasePath,
                unitDefinition.Sprites.MaskPath);
        }

        var buildingsIntact = new Dictionary<ContentId, TeamSprite>();
        var buildingsRuined = new Dictionary<ContentId, TeamSprite>();
        foreach (var buildingDefinition in catalog.BuildingsModule.BuildingsById.Values)
        {
            buildingsIntact[buildingDefinition.ContentId] = ResolvePair(
                buildingDefinition.ContentId,
                buildingDefinition.SourceModuleRootPath,
                buildingDefinition.Sprites.BasePath,
                buildingDefinition.Sprites.MaskPath);

            if (!string.IsNullOrWhiteSpace(buildingDefinition.Sprites.RuinedBasePath)
                && !string.IsNullOrWhiteSpace(buildingDefinition.Sprites.RuinedMaskPath))
            {
                buildingsRuined[buildingDefinition.ContentId] = LoadModulePair(
                    buildingDefinition.SourceModuleRootPath,
                    buildingDefinition.Sprites.RuinedBasePath,
                    buildingDefinition.Sprites.RuinedMaskPath);
            }
        }

        var atlas = new MatchTextureAtlas(
            grass: LoadThemeTexture(theme.TerrainRelativePath("grass.png")),
            water: LoadThemeTexture(theme.TerrainRelativePath("water.png")),
            road: LoadThemeTexture(theme.TerrainRelativePath("road.png")),
            mountain: LoadThemeTexture(theme.TerrainRelativePath("mountain.png")),
            bridge: LoadThemeTexture(theme.TerrainRelativePath("bridge.png")),
            forest: LoadThemeTexture(theme.TerrainRelativePath("forest.png")),
            gravestone: LoadThemeTexture(theme.GravestoneRelativePath),
            units: units,
            buildingsIntact: buildingsIntact,
            buildingsRuined: buildingsRuined);

        atlas._owned.AddRange(ownedTextures);
        return atlas;
    }

    public void Dispose()
    {
        foreach (var texture in _owned)
            texture.DisposeIfOwned();
        _owned.Clear();
    }
}
