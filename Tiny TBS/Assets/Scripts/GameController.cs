using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Config;
using Assets.Scripts.Input;
using HUD.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    [SerializeField] private GridDrawer _gridDrawer;
    [SerializeField] private MouseController _mouseController;
    [SerializeField] private MenuController _menuController;
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private TilesConfig _tilesConfig;
    [SerializeField] private GameObject _unitPrefab;
    private Camera _camera;
    private Map _map;
    public bool randomMap;
    
    private MouseInteractionState _mouseInteractionState = MouseInteractionState.Idle;
    private Unit _selectedUnit;
    private Vector2Int _selectedCoord;
    private UnitAction _selectedAction;

    void PlaceUnit(Unit unit)
    {
        Instantiate(_unitPrefab)
            .GetComponent<GameUnitController>()
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

    void OnSelectUnit(Vector3 mousePosition)
    {
        Unit unit = null;
        
        var coord = FieldUtils.GetCoordFromMousePos(mousePosition, _camera);
        
        // TODO: add check for map size
        var isValidCoord = coord.x >= 0 || coord.y >= 0;
        if (isValidCoord)
        {
            _selectedUnit = _map[coord].Unit;
        }

        SetMouseInteractionState(_selectedUnit != null
            ? MouseInteractionState.SelectingAction
            : MouseInteractionState.Idle);
    }

    private void OnMoveUnit()
    {
        _map[_selectedUnit.Coord].Unit = null;
        _selectedUnit.Coord = _selectedCoord;
        _map[_selectedUnit.Coord].Unit = _selectedUnit;
    }

    private void OnSelectCoord()
    {
        _gridDrawer.ShowGrid(GetMoveCoordsForUnit(_selectedUnit));
    }

    private IEnumerable<Vector2Int> GetMoveCoordsForUnit(Unit unit) => FieldUtils.GetNeighbours(unit.Coord);

    private void OnCoordSelected()
    {
        switch (_selectedAction)
        {
            case UnitAction.Move:
                var coords = GetMoveCoordsForUnit(_selectedUnit).ToHashSet();
                if (!coords.Contains(_selectedCoord))
                {
                    SetMouseInteractionState(MouseInteractionState.Idle);
                    return;
                }
                
                OnMoveUnit();
                SetMouseInteractionState(MouseInteractionState.WaitingForAnimations);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerable<MenuItem> GetUnitMenu()
    {
        yield return new MenuItem()
        {
            title = "Move",
            onClick = () => SetMouseInteractionState(MouseInteractionState.SelectingCoord)
        };
    }

    private void SetMouseInteractionState(MouseInteractionState state)
    {
        if (_mouseInteractionState == state) return;
        
        _mouseInteractionState = state;
        
        Debug.Log($"MouseInteractionState: {state}");
        // _cameraController.enabled = state != MouseInteractionState.SelectingAction;

        switch (state)
        {
            case MouseInteractionState.Idle:
                _selectedUnit = null;
                _menuController.Hide();
                _gridDrawer.Hide();
                break;
            case MouseInteractionState.SelectingAction:
                _menuController.ShowMenu(Input.mousePosition, GetUnitMenu());
                break;
            case MouseInteractionState.SelectingCoord:
                OnSelectCoord();
                break;
            case MouseInteractionState.WaitingForAnimations:
                // TODO: wait for animations, for now just get back to idle
                SetMouseInteractionState(MouseInteractionState.Idle);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        
    }

    private void OnMouseClick(Vector3 position)
    {
        if (EventSystem.current.currentSelectedGameObject != null) return;
        if (_mouseInteractionState == MouseInteractionState.Idle)
        {
            OnSelectUnit(position);
        }
        else if (_mouseInteractionState == MouseInteractionState.SelectingCoord)
        {
            _selectedCoord = FieldUtils.GetCoordFromMousePos(position, _camera);
            OnCoordSelected();
        }
        else if (_mouseInteractionState == MouseInteractionState.SelectingAction)
        {
            SetMouseInteractionState(MouseInteractionState.Idle);
        }
    }

    void Awake()
    {
        _camera = Camera.main;
        _mouseController.onClick += pos => OnMouseClick(pos);
        _mouseController.onDrag += data => OnDrag(data);
        
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

    private void OnDrag(MouseController.DragData data)
    {
        if (_mouseInteractionState == MouseInteractionState.SelectingAction)
        {
            SetMouseInteractionState(MouseInteractionState.Idle);
        }
    }

    public enum MouseInteractionState
    {
        Idle,
        SelectingAction,
        SelectingCoord,
        WaitingForAnimations
    }

    public enum UnitAction
    {
        Move
    }
}