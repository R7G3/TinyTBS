using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Game.Assets;

namespace TinyTBS.Game.Match;

/// <summary>Loaded textures for the match (terrain + base/mask pairs).</summary>
public sealed class MatchTextureAtlas : IDisposable
{
    private readonly List<LoadedTexture> _owned = [];
    private readonly Dictionary<UnitKind, TeamSprite> _units;
    private readonly TeamSprite _castle;
    private readonly TeamSprite _village;
    private readonly TeamSprite _villageRuined;

    private MatchTextureAtlas(
        Texture2D grass,
        Texture2D water,
        Texture2D road,
        Texture2D mountain,
        Texture2D bridge,
        Texture2D forest,
        Dictionary<UnitKind, TeamSprite> units,
        TeamSprite castle,
        TeamSprite village,
        TeamSprite villageRuined)
    {
        Grass = grass;
        Water = water;
        Road = road;
        Mountain = mountain;
        Bridge = bridge;
        Forest = forest;
        _units = units;
        _castle = castle;
        _village = village;
        _villageRuined = villageRuined;
    }

    public Texture2D Grass { get; }
    public Texture2D Water { get; }
    public Texture2D Road { get; }
    public Texture2D Mountain { get; }
    public Texture2D Bridge { get; }
    public Texture2D Forest { get; }

    public TeamSprite King => Unit(UnitKind.King);
    public TeamSprite Swordsman => Unit(UnitKind.Swordsman);
    public TeamSprite Castle => _castle;
    public TeamSprite Village => _village;

    public Texture2D Terrain(TerrainKind kind) => kind switch
    {
        TerrainKind.Water => Water,
        TerrainKind.Road => Road,
        TerrainKind.Mountain => Mountain,
        TerrainKind.Bridge => Bridge,
        TerrainKind.Forest => Forest,
        _ => Grass,
    };

    public TeamSprite Unit(UnitKind kind) =>
        _units.TryGetValue(kind, out var sprite) ? sprite : _units[UnitKind.Swordsman];

    public TeamSprite Building(BuildingKind kind, bool isRuined = false) => kind switch
    {
        BuildingKind.Castle => _castle,
        BuildingKind.Village when isRuined => _villageRuined,
        _ => _village,
    };

    public static MatchTextureAtlas Load(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        IAssetResolver assets)
    {
        var ownedTextures = new List<LoadedTexture>();

        LoadedTexture LoadTexture(string logicalRelativePath, string contentAssetName)
        {
            var loaded = GameTextureLoader.LoadOrFallback(
                graphicsDevice, content, assets, logicalRelativePath, contentAssetName);
            ownedTextures.Add(loaded);
            return loaded;
        }

        TeamSprite LoadTeamSprite(string folder, string assetId)
        {
            var baseTexture = LoadTexture(
                $"Images/{folder}/{assetId}_base.png",
                $"Images/{folder}/{assetId}_base");
            var maskTexture = LoadTexture(
                $"Images/{folder}/{assetId}_mask.png",
                $"Images/{folder}/{assetId}_mask");
            return new TeamSprite(baseTexture.Texture, maskTexture.Texture);
        }

        var units = new Dictionary<UnitKind, TeamSprite>
        {
            [UnitKind.King] = LoadTeamSprite("units", "king"),
            [UnitKind.Swordsman] = LoadTeamSprite("units", "swordsman"),
            [UnitKind.Archer] = LoadTeamSprite("units", "archer"),
            [UnitKind.Lizard] = LoadTeamSprite("units", "lizard"),
            [UnitKind.Witch] = LoadTeamSprite("units", "witch"),
            [UnitKind.Wisp] = LoadTeamSprite("units", "wisp"),
            [UnitKind.Golem] = LoadTeamSprite("units", "golem"),
            [UnitKind.Catapult] = LoadTeamSprite("units", "catapult"),
            [UnitKind.Wyvern] = LoadTeamSprite("units", "wyvern"),
            [UnitKind.Skeleton] = LoadTeamSprite("units", "skeleton"),
        };

        var atlas = new MatchTextureAtlas(
            grass: LoadTexture("Images/terrain/grass.png", "Images/terrain/grass").Texture,
            water: LoadTexture("Images/terrain/water.png", "Images/terrain/water").Texture,
            road: LoadTexture("Images/terrain/road.png", "Images/terrain/road").Texture,
            mountain: LoadTexture("Images/terrain/mountain.png", "Images/terrain/mountain").Texture,
            bridge: LoadTexture("Images/terrain/bridge.png", "Images/terrain/bridge").Texture,
            forest: LoadTexture("Images/terrain/forest.png", "Images/terrain/forest").Texture,
            units: units,
            castle: LoadTeamSprite("buildings", "castle"),
            village: LoadTeamSprite("buildings", "village"),
            villageRuined: LoadTeamSprite("buildings", "village_ruined"));

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
