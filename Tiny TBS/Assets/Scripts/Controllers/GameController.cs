using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.Buildings;
using Assets.Scripts.Configs;
using Assets.Scripts.HUD;
using Assets.Scripts.HUD.Menu;
using Assets.Scripts.PlayerAction;
using Assets.Scripts.Tiles;
using Assets.Scripts.Units;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Controllers
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private GridDrawer _gridDrawer;
        [SerializeField] private MouseController _mouseController;
        [SerializeField] private MenuController _menuController;
        [SerializeField] private TileInformationVisibilityController _widgetVisibility;
        [SerializeField] private TileInfoController _terrainInfo;
        [SerializeField] private TileInfoController _unitInfo;
        [SerializeField] private TileInfoController _buildInfo;
        [SerializeField] private HUDMessageController _hudMessageController;
        [SerializeField] private TilesConfig _tilesConfig;
        [SerializeField] private UnitController _unitController;
        [SerializeField] private BalanceConfig _balanceConfig;
        [SerializeField] private bool _randomMap;

        private UIController _uiController;
        private Camera _camera;
        private Map _map;
        private readonly List<UniTask> _queuedAnimations = new();
        private List<Player> _players;
        private List<Unit> _units = new();

        void SetPlayers(IEnumerable<Player> players)
        {
            _players = players.ToList();
        }

        void PlaceUnit(Unit unit)
        {
            _units.Add(unit);
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

            const float halfTileHeight = 0.07f;
            var tileView = Instantiate(
                    tileResource,
                    FieldUtils.GetWorldPos(new Vector2Int(x, y)) + Vector3.down * halfTileHeight,
                    Quaternion.identity)
                .GetComponent<TileView>();

            tileView.SetType(type);

            return tileView;
        }


        private void OnMoveUnit(MoveUnit moveUnit)
        {
            _map[moveUnit.unit.Coord].Unit = null;
            moveUnit.unit.Coord = moveUnit.coord;
            _map[moveUnit.coord].Unit = moveUnit.unit;
            moveUnit.unit.HasMoved = true;

            var moveTask = _unitController.MoveUnit(moveUnit.unit, Enumerable.Repeat(moveUnit.coord, 1));
            _queuedAnimations.Add(moveTask);
        }

        private void Awake()
        {
            _camera = Camera.main;

            if (_randomMap)
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

            _uiController = new UIController(
                _map,
                _gridDrawer,
                _menuController,
                _camera,
                _hudMessageController,
                _balanceConfig,
                _terrainInfo,
                _buildInfo,
                _unitInfo,
                _widgetVisibility);

            _mouseController.onClick += _uiController.OnMouseClick;
            _mouseController.onMouseMove += _uiController.OnMouseMove;
            _mouseController.onDrag += _uiController.OnMouseDrag;

            SetPlayers(new[]
            {
                new Player(new Fraction("Player 1")), 
                new Player(new Fraction("Player 2"))
            });

            var unit = new Unit
            {
                Fraction = _players[0].Fraction,
                Coord = new Vector2Int(1, 1)
            };

            unit.Speed = 3;

            PlaceUnit(unit);
            
            unit = new Unit
            {
                Fraction = _players[1].Fraction,
                Coord = new Vector2Int(3, 3)
            };

            unit.Speed = 3;

            PlaceUnit(unit);
            
            
            PlaceBuilding(_players[0].Fraction, BuildingType.Village, new Vector2Int(0, 1));
            PlaceBuilding(_players[1].Fraction, BuildingType.Village, new Vector2Int(3, 1));
            PlaceBuilding(_players[0].Fraction, BuildingType.Castle, new Vector2Int(0, 2));
            PlaceBuilding(_players[1].Fraction, BuildingType.Castle, new Vector2Int(3, 2));
            

            StartCoroutine(Turn().AsUniTask().ToCoroutine());
        }

        private void PlaceBuilding(IFraction fraction, BuildingType type, Vector2Int coord)
        {
            Destroy(((TileView)_map[coord]).gameObject);
            _map[coord] = CreateTile(coord.x, coord.y, TileType.Grass);
            _map[coord].Building = new Building()
            {
                Fraction = fraction,
                State = new BuildingState(),
                Type = type
            };
        }

        // ReSharper disable once FunctionNeverReturns
        private async Task Turn()
        {
            var currentPlayerIndex = 0;
            while (true)
            {
                var currentPlayer = _players[currentPlayerIndex];

                TurnStart(currentPlayer);

                IPlayerAction playerAction;
                do
                {
                    playerAction = await _uiController.GetPlayerAction(currentPlayer);

                    if (playerAction == null) continue;

                    ProcessPlayerAction(playerAction);
                    await UniTask.WhenAll(_queuedAnimations);
                    _queuedAnimations.Clear();
                } while (playerAction is not PlayerAction.EndTurn && CanDoMoreActions(currentPlayer));

                await _uiController.ShowMessage("Next turn");
                
                
                currentPlayerIndex = (currentPlayerIndex + 1) % _players.Count;
            }
        }

        private IEnumerable<Unit> GetPlayerUnits(Player player)
            => _units.Where(u => u.Fraction == player.Fraction);

        private static bool IsUnitEnabled(Unit unit)
            // For future
            // => !unit.HasMoved || !unit.HasPerformedAction;
            => !unit.HasMoved;

        private bool CanDoMoreActions(Player player) => GetPlayerUnits(player).Any(IsUnitEnabled);

        private void TurnStart(Player player)
        {
            foreach (var unit in GetPlayerUnits(player))
            {
                unit.HasMoved = false;
                unit.HasPerformedAction = false;
            }
        }
        
        private void ProcessPlayerAction(IPlayerAction playerAction)
        {
            switch (playerAction)
            {
                case MoveUnit moveUnit:
                    OnMoveUnit(moveUnit);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(playerAction));
            }
        }
    }
}