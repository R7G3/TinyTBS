using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>Loaded textures for the match (terrain + base/mask pairs from content modules).</summary>
public sealed class MatchTextureAtlas : IDisposable
{
    private readonly List<LoadedTexture> _owned = [];
    private readonly Dictionary<UnitKind, TeamSprite> _units;
    private readonly Dictionary<BuildingKind, TeamSprite> _buildingsIntact;
    private readonly Dictionary<BuildingKind, TeamSprite> _buildingsRuined;

    private MatchTextureAtlas(
        Texture2D grass,
        Texture2D water,
        Texture2D road,
        Texture2D mountain,
        Texture2D bridge,
        Texture2D forest,
        Dictionary<UnitKind, TeamSprite> units,
        Dictionary<BuildingKind, TeamSprite> buildingsIntact,
        Dictionary<BuildingKind, TeamSprite> buildingsRuined)
    {
        Grass = grass;
        Water = water;
        Road = road;
        Mountain = mountain;
        Bridge = bridge;
        Forest = forest;
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

    public Texture2D Terrain(TerrainKind kind) => kind switch
    {
        TerrainKind.Water => Water,
        TerrainKind.Road => Road,
        TerrainKind.Mountain => Mountain,
        TerrainKind.Bridge => Bridge,
        TerrainKind.Forest => Forest,
        _ => Grass,
    };

    public TeamSprite Unit(UnitKind kind)
    {
        if (_units.TryGetValue(kind, out var sprite))
            return sprite;

        throw new InvalidOperationException($"No unit sprite loaded for '{kind}'.");
    }

    public TeamSprite Building(BuildingKind kind, bool isRuined = false)
    {
        if (isRuined && _buildingsRuined.TryGetValue(kind, out var ruined))
            return ruined;

        if (_buildingsIntact.TryGetValue(kind, out var intact))
            return intact;

        throw new InvalidOperationException($"No building sprite loaded for '{kind}'.");
    }

    public static MatchTextureAtlas Load(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        IAssetResolver assets,
        MatchContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var ownedTextures = new List<LoadedTexture>();

        LoadedTexture LoadBundled(string logicalRelativePath, string contentAssetName)
        {
            var loaded = GameTextureLoader.LoadOrFallback(
                graphicsDevice, content, assets, logicalRelativePath, contentAssetName);
            ownedTextures.Add(loaded);
            return loaded;
        }

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

        var units = new Dictionary<UnitKind, TeamSprite>();
        foreach (var (unitKind, unitDefinition) in EnumerateUnits(catalog))
        {
            if (unitDefinition.Sprites is null)
            {
                throw new InvalidOperationException(
                    $"Unit '{unitDefinition.ContentId.Full}' has no sprites in the units module.");
            }

            units[unitKind] = LoadModulePair(
                catalog.UnitsModule.ModuleRootPath,
                unitDefinition.Sprites.BasePath,
                unitDefinition.Sprites.MaskPath);
        }

        var buildingsIntact = new Dictionary<BuildingKind, TeamSprite>();
        var buildingsRuined = new Dictionary<BuildingKind, TeamSprite>();
        foreach (var (buildingKind, buildingDefinition) in EnumerateBuildings(catalog))
        {
            buildingsIntact[buildingKind] = LoadModulePair(
                catalog.BuildingsModule.ModuleRootPath,
                buildingDefinition.Sprites.BasePath,
                buildingDefinition.Sprites.MaskPath);

            if (!string.IsNullOrWhiteSpace(buildingDefinition.Sprites.RuinedBasePath)
                && !string.IsNullOrWhiteSpace(buildingDefinition.Sprites.RuinedMaskPath))
            {
                buildingsRuined[buildingKind] = LoadModulePair(
                    catalog.BuildingsModule.ModuleRootPath,
                    buildingDefinition.Sprites.RuinedBasePath,
                    buildingDefinition.Sprites.RuinedMaskPath);
            }
        }

        var atlas = new MatchTextureAtlas(
            grass: LoadBundled("Images/terrain/grass.png", "Images/terrain/grass").Texture,
            water: LoadBundled("Images/terrain/water.png", "Images/terrain/water").Texture,
            road: LoadBundled("Images/terrain/road.png", "Images/terrain/road").Texture,
            mountain: LoadBundled("Images/terrain/mountain.png", "Images/terrain/mountain").Texture,
            bridge: LoadBundled("Images/terrain/bridge.png", "Images/terrain/bridge").Texture,
            forest: LoadBundled("Images/terrain/forest.png", "Images/terrain/forest").Texture,
            units: units,
            buildingsIntact: buildingsIntact,
            buildingsRuined: buildingsRuined);

        atlas._owned.AddRange(ownedTextures);
        return atlas;
    }

    private static IEnumerable<(UnitKind Kind, UnitDefinition Definition)> EnumerateUnits(
        MatchContentCatalog catalog)
    {
        foreach (UnitKind kind in Enum.GetValues<UnitKind>())
        {
            if (catalog.TryGetUnit(kind, out var definition))
                yield return (kind, definition);
        }
    }

    private static IEnumerable<(BuildingKind Kind, BuildingDefinition Definition)> EnumerateBuildings(
        MatchContentCatalog catalog)
    {
        foreach (BuildingKind kind in Enum.GetValues<BuildingKind>())
        {
            if (catalog.TryGetBuilding(kind, out var definition))
                yield return (kind, definition);
        }
    }

    public void Dispose()
    {
        foreach (var texture in _owned)
            texture.DisposeIfOwned();
        _owned.Clear();
    }
}
