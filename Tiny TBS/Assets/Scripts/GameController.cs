using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Config;
using Assets.Scripts.Input;
using UnityEngine;
using Utils;
using Object = System.Object;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    [SerializeField] private GridDrawer _gridDrawer;
    [SerializeField] private MouseController _mouseController;
    [SerializeField] private TilesConfig _tilesConfig;
    [SerializeField] private GameObject _unitPrefab;
    private Camera _camera;
    private Map _map;
    private Plane _plane;
    public bool randomMap;

    void PlaceUnit(Unit unit)
    {
        Instantiate(_unitPrefab)
            .AddComponent<GameUnitController>()
            .Init(unit);

        _map[unit.Coord].Unit = unit;
    }

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

        Destroy(map[1,1].gameObject);
        map[1, 1] = CreateTile(1, 1, TileType.Grass);

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

        // tileView.SetBuilding(new Building
        // {
        //     Type = BuildingType.Castle
        // });

        return tileView;
    }

    void OnMouseClick(Vector3 position)
    {
        var ray = _camera.ScreenPointToRay(position);

        if (!_plane.Raycast(ray, out var distance))
        {
            return;
        }

        var worldPosition = ray.GetPoint(distance);
        var coord = FieldUtils.To2dCoord(worldPosition);

        Debug.Log($"Clicked: {{x:{coord.x},y:{coord.y}}}");

        Unit unit;

        unit = _map[coord].Unit;

        if (unit == null)
        {
            return;
        }

        var neighbournFields = new Vector2Int[]
        {
            new Vector2Int(coord.x + 1, coord.y + 1),
            new Vector2Int(coord.x, coord.y + 1),
            new Vector2Int(coord.x - 1, coord.y + 1),
            new Vector2Int(coord.x + 1, coord.y),
            new Vector2Int(coord.x - 1, coord.y),
            new Vector2Int(coord.x + 1, coord.y - 1),
            new Vector2Int(coord.x, coord.y - 1),
            new Vector2Int(coord.x - 1, coord.y - 1),
        };

        _gridDrawer.ShowGrid(neighbournFields);
    }

    void Awake()
    {
        _camera = Camera.main;
        _plane = new Plane(Vector3.up, 0); // zero on y
        _mouseController.onClick += pos => OnMouseClick(pos);
        
        if (randomMap)
        {
            _map = CreateRandomMap(20);
        }
        else
        {
            _map = CreateMap(
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

        var unit = new Unit
        {
            Coord = new Vector2Int(1, 1)
        };

        PlaceUnit(unit);
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