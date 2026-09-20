using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Game.Assets;

namespace TinyTBS.Game.Match;

/// <summary>Loaded textures for the match (terrain + base/mask pairs).</summary>
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
        // Forest uses grass art until a dedicated terrain tile exists.
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

        var atlas = new MatchTextureAtlas(
            grass: LoadTexture("Images/terrain/grass.png", "Images/terrain/grass").Texture,
            water: LoadTexture("Images/terrain/water.png", "Images/terrain/water").Texture,
            road: LoadTexture("Images/terrain/road.png", "Images/terrain/road").Texture,
            mountain: LoadTexture("Images/terrain/mountain.png", "Images/terrain/mountain").Texture,
            bridge: LoadTexture("Images/terrain/bridge.png", "Images/terrain/bridge").Texture,
            king: LoadTeamSprite("units", "king"),
            swordsman: LoadTeamSprite("units", "swordsman"),
            castle: LoadTeamSprite("buildings", "castle"),
            village: LoadTeamSprite("buildings", "village"));

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
