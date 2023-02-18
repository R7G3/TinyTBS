using System;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts;
using Assets.Scripts.Config;
using Assets.Scripts.Input;
using Cysharp.Threading.Tasks;
using HUD.Menu;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    [SerializeField] private GridDrawer _gridDrawer;
    [SerializeField] private MouseController _mouseController;
    [SerializeField] private MenuController _menuController;
    [SerializeField] private TilesConfig _tilesConfig;
    [SerializeField] private GameObject _unitPrefab;
    private Camera _camera;
    private Map _map;
    public bool randomMap;
    
    private UnitController _unitController;
    private UIController _uiController;

    void PlaceUnit(Unit unit)
    {
        _map[unit.Coord].Unit = unit;
        _unitController.CreateUnitAt(unit, unit.Coord);
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


    private void OnMoveUnit(Unit unit, Vector2Int coord)
    {
        _map[unit.Coord].Unit = null;
        unit.Coord = coord;
        _map[unit.Coord].Unit = unit;

        _unitController.MoveUnit(unit, Enumerable.Repeat<Vector2Int>(coord, 1));
    }

    private void Awake()
    {
        _camera = Camera.main;
        
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
        
        _uiController = new UIController(_map, _gridDrawer, _menuController, _camera);
        _uiController.onMoveUnit += OnMoveUnit;
        _unitController = new UnitController(_unitPrefab);
        _mouseController.onClick += pos => _uiController.OnMouseClick(pos);
        _mouseController.onMouseMove += pos => _uiController.OnMouseMove(pos);

        var unit = new Unit
        {
            Coord = new Vector2Int(1, 1)
        };

        PlaceUnit(unit);

        StartCoroutine(Turn().AsUniTask().ToCoroutine());
    }
    
    private async Task Turn()
    {
        while (true)
        {
            await _uiController.StartIdleScenario();
        }
    }
}