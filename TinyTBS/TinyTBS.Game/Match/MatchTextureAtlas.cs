using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Game.Assets;

namespace TinyTBS.Game.Match;

/// <summary>Loaded textures for the demo match (terrain + base/mask pairs).</summary>
public sealed class MatchTextureAtlas : IDisposable
{
    private readonly List<LoadedTexture> _owned = [];

    private MatchTextureAtlas(
        Texture2D grass,
        Texture2D water,
        Texture2D road,
        Texture2D mountain,
        Texture2D bridge,
        TeamSprite king,
        TeamSprite swordsman,
        TeamSprite castle,
        TeamSprite village)
    {
        Grass = grass;
        Water = water;
        Road = road;
        Mountain = mountain;
        Bridge = bridge;
        King = king;
        Swordsman = swordsman;
        Castle = castle;
        Village = village;
    }

    public Texture2D Grass { get; }
    public Texture2D Water { get; }
    public Texture2D Road { get; }
    public Texture2D Mountain { get; }
    public Texture2D Bridge { get; }
    public TeamSprite King { get; }
    public TeamSprite Swordsman { get; }
    public TeamSprite Castle { get; }
    public TeamSprite Village { get; }

    public Texture2D Terrain(TerrainKind kind) => kind switch
    {
        TerrainKind.Water => Water,
        TerrainKind.Road => Road,
        TerrainKind.Mountain => Mountain,
        TerrainKind.Bridge => Bridge,
        _ => Grass,
    };

    public TeamSprite Unit(UnitKind kind) => kind switch
    {
        UnitKind.King => King,
        _ => Swordsman,
    };

    public TeamSprite Building(BuildingKind kind) => kind switch
    {
        BuildingKind.Castle => Castle,
        _ => Village,
    };

    public static MatchTextureAtlas Load(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        IAssetResolver assets)
    {
        var owned = new List<LoadedTexture>();

        LoadedTexture Load(string logical, string contentName)
        {
            var loaded = GameTextureLoader.LoadOrFallback(
                graphicsDevice, content, assets, logical, contentName);
            owned.Add(loaded);
            return loaded;
        }

        TeamSprite LoadPair(string folder, string id)
        {
            var baseTex = Load($"Images/{folder}/{id}_base.png", $"Images/{folder}/{id}_base");
            var maskTex = Load($"Images/{folder}/{id}_mask.png", $"Images/{folder}/{id}_mask");
            return new TeamSprite(baseTex.Texture, maskTex.Texture);
        }

        var atlas = new MatchTextureAtlas(
            grass: Load("Images/terrain/grass.png", "Images/terrain/grass").Texture,
            water: Load("Images/terrain/water.png", "Images/terrain/water").Texture,
            road: Load("Images/terrain/road.png", "Images/terrain/road").Texture,
            mountain: Load("Images/terrain/mountain.png", "Images/terrain/mountain").Texture,
            bridge: Load("Images/terrain/bridge.png", "Images/terrain/bridge").Texture,
            king: LoadPair("units", "king"),
            swordsman: LoadPair("units", "swordsman"),
            castle: LoadPair("buildings", "castle"),
            village: LoadPair("buildings", "village"));

        atlas._owned.AddRange(owned);
        return atlas;
    }

    public void Dispose()
    {
        foreach (var texture in _owned)
            texture.DisposeIfOwned();
        _owned.Clear();
    }

    public readonly struct TeamSprite(Texture2D baseTexture, Texture2D maskTexture)
    {
        public Texture2D Base { get; } = baseTexture;
        public Texture2D Mask { get; } = maskTexture;
    }
}
