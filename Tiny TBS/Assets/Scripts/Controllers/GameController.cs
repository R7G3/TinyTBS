using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.Buildings;
using Assets.Scripts.Configs;
using Assets.Scripts.GameLogic;
using Assets.Scripts.HUD;
using Assets.Scripts.HUD.Menu;
using Assets.Scripts.PlayerAction;
using Assets.Scripts.Tiles;
using Assets.Scripts.Units;
using Assets.Scripts.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Controllers
{
    public class GameController : MonoBehaviour, IService
    {
        private MouseController _mouseController;
        private TilesConfig _tilesConfig;
        private UnitController _unitController;
        private BalanceConfig _balanceConfig;

        [SerializeField] private bool _randomMap;
        [SerializeField] private ServiceLocator _serviceLocator;

        private Attack _attackLogic;
        private UIController _uiController;
        private Map _map;
        private readonly List<UniTask> _queuedAnimations = new();
        private List<Player> _players;
        private readonly List<Unit> _units = new();

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

        void RemoveUnit(Unit unit)
        {
            _units.Remove(unit);
            _map[unit.Coord].Unit = null;
            _unitController.RemoveUnitAt(unit);
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

        private async UniTask OnAttackUnit(AttackUnit attackUnit)
        {
            await ProcessPlayerAction(new MoveUnit
            {
                coord = attackUnit.StandingCoord,
                unit = attackUnit.Attacker
            });
            
            var damage = _attackLogic.CalculateDamage(attackUnit.Attacker, attackUnit.Defender);

            if (attackUnit.Defender.Health < damage)
            {
                RemoveUnit(attackUnit.Defender);
                return;
            }

            await _unitController.Attack(attackUnit.Attacker, attackUnit.Defender.Coord);

            attackUnit.Defender.Health -= damage;

            var retaliatoryDamage = _attackLogic.CalculateDamage(attackUnit.Defender, attackUnit.Attacker);

            if (attackUnit.Attacker.Health < retaliatoryDamage)
            {
                RemoveUnit(attackUnit.Attacker);
                return;
            }
            
            await _unitController.Attack(attackUnit.Defender, attackUnit.Attacker.Coord);

            attackUnit.Attacker.Health -= retaliatoryDamage;

            attackUnit.Attacker.HasMoved = true;
        }

        private async Task OnOccupyBuilding(OccupyBuilding occupyBuilding)
        {
            var tile = _map[occupyBuilding.Coord];
            var building = tile.Building;
            if (building == null)
            {
                throw new InvalidOperationException(
                    $"Trying to occupy building that does not exist on coord {occupyBuilding.Coord}");
            }
            
            await ProcessPlayerAction(new MoveUnit
            {
                coord = occupyBuilding.Coord,
                unit = occupyBuilding.Unit
            });

            building.Owner = occupyBuilding.Unit.Owner;
            occupyBuilding.Unit.HasPerformedAction = true;
        }

        private void Awake()
        {
            _serviceLocator.Register(() =>
            {
                return _randomMap switch
                {
                    true => CreateRandomMap(20),
                    _ => CreateMap(new TileType[,]
                    {
                        { TileType.Grass, TileType.Road, TileType.Mountain, },
                        { TileType.Water, TileType.Grass, TileType.Road, },
                        { TileType.Road, TileType.Water, TileType.Grass, }
                    })
                };
            });
        }

        private void Start()
        {
            _mouseController = _serviceLocator.GetService<MouseController>();
            _tilesConfig = _serviceLocator.GetService<TilesConfig>();
            _unitController = _serviceLocator.GetService<UnitController>();
            _balanceConfig = _serviceLocator.GetService<BalanceConfig>();
            _map = _serviceLocator.GetService<Map>();
            _uiController = _serviceLocator.GetService<UIController>();


            _attackLogic = new Attack(_map, _balanceConfig);

            _mouseController.onClick += _uiController.OnMouseClick;
            _mouseController.onMouseMove += _uiController.OnMouseMove;
            _mouseController.onDrag += _uiController.OnMouseDrag;

            SetPlayers(new[]
            {
                new Player(id: "Player 1", name: "Player 1"),
                new Player(id: "Player 2", name: "Player 2")
            });

            var unit = new Unit(
                _players[0],
                new Vector2Int(1, 1));
            PlaceUnit(unit);

            unit = new Unit(
                _players[1],
                new Vector2Int(3, 3));
            PlaceUnit(unit);


            PlaceBuilding(_players[0], BuildingType.Village, new Vector2Int(0, 1));
            PlaceBuilding(_players[1], BuildingType.Village, new Vector2Int(3, 1));
            PlaceBuilding(_players[0], BuildingType.Castle, new Vector2Int(0, 2));
            PlaceBuilding(_players[1], BuildingType.Castle, new Vector2Int(3, 2));


            StartCoroutine(Turn().AsUniTask().ToCoroutine());
        }

        private void PlaceBuilding(Player owner, BuildingType type, Vector2Int coord)
        {
            Destroy(((TileView)_map[coord]).gameObject);
            _map[coord] = CreateTile(coord.x, coord.y, TileType.Grass);
            _map[coord].Building = new Building()
            {
                Owner = owner,
                State = new BuildingState(),
                Type = type,
                Coord = coord
            };
        }

        private async UniTask WaitAnimations()
        {
            await UniTask.WhenAll(_queuedAnimations);
            _queuedAnimations.Clear();
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

                    await ProcessPlayerAction(playerAction);

                } while (playerAction is not PlayerAction.EndTurn && CanDoMoreActions(currentPlayer));

                await _uiController.ShowMessage("Next turn");


                currentPlayerIndex = (currentPlayerIndex + 1) % _players.Count;
            }
        }

        private IEnumerable<Unit> GetPlayerUnits(Player player)
            => _units.Where(u => u.Owner == player);

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

        private async Task ProcessPlayerAction(IPlayerAction playerAction)
        {
            switch (playerAction)
            {
                case MoveUnit moveUnit:
                    OnMoveUnit(moveUnit);
                    break;
                case AttackUnit attackUnit:
                    await OnAttackUnit(attackUnit);
                    break;
                case OccupyBuilding occupyBuilding:
                    await OnOccupyBuilding(occupyBuilding);
                    break;
                case BuyUnit buyUnit:
                    PlaceUnit(buyUnit.Unit);
                    break;
                case EndTurn _:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(playerAction));
            }

            await WaitAnimations();
        }
    }
}