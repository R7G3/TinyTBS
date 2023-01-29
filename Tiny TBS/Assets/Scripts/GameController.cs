using System;
using Assets.Scripts;
using Assets.Scripts.Config;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    private TilesConfig _tilesConfig;
    public bool randomMap;

    Map CreateRandomMap(int size)
    {
        var width = size;
        var height = size;

        var map = new Map(width, height);
        var typeValues = Enum.GetValues(typeof(TileType));

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var rndIndex = Random.Range(0, typeValues.Length);
                map[x, y] = CreateTile(x, y, (TileType)typeValues.GetValue(rndIndex));
            }
        }

        return map;
    }

    Map CreateMap(TileType[,] tileTypes)
    {
        var width = tileTypes.GetLength(0);
        var height = tileTypes.GetLength(0);

        var map = new Map(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x, y] = CreateTile(x, y, tileTypes[x, y]);
            }
        }

        return map;
    }

    GameObject GetTilePrefab(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Road:
                return _tilesConfig.tilePrefabs.road;
            case TileType.Grass:
                return _tilesConfig.tilePrefabs.grass;
            case TileType.Mountain:
                return _tilesConfig.tilePrefabs.mountain;
            case TileType.Water:
                return _tilesConfig.tilePrefabs.water;
            default:
                throw new ArgumentOutOfRangeException(nameof(tileType), tileType, null);
        }
    }

    TileView CreateTile(int x, int y, TileType type)
    {
        var vector = new Vector3(x, 0, y);

        var tileResource = GetTilePrefab(type);

        var tileView = Instantiate(
                tileResource,
                vector,
                Quaternion.identity)
            .GetComponent<TileView>();

        tileView.SetType(type);
        // tileView.SetBuilding(new Building
        // {
        //     Type = BuildingType.Castle
        // });

        return tileView;
    }

    void Awake()
    {
        _tilesConfig = Resources.Load<TilesConfig>("TilesConfig");
        if (randomMap)
        {
            CreateRandomMap(20);
        }
        else
        {
            CreateMap(
                new TileType[,]
                {
                    {
                        TileType.Grass,
                        TileType.Road,
                        TileType.Mountain,
                    },
                    {
                        TileType.Water,
                        TileType.Grass,
                        TileType.Road,
                    },
                    {
                        TileType.Road,
                        TileType.Water,
                        TileType.Grass,
                    }
                });
        }
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }
}