using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Config;
using Assets.Scripts.Input;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    [SerializeField] private GridDrawer _gridDrawer;
    [SerializeField] private MouseController _mouseController;
    [SerializeField] private TilesConfig _tilesConfig;
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
        var tileResource = GetTilePrefab(type);

        var tileView = Instantiate(
                tileResource,
                FieldUtils.GetWorldPos(new Vector2Int(x, y)),
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
        _mouseController.onClick += pos => _gridDrawer.SelectGridRect(pos);
        
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
        _gridDrawer.ShowGrid(new List<Vector2Int>()
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, 2),
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(1, 2),
        });
    }

    // Update is called once per frame
    void Update()
    {
    }
}