using Assets.Scripts.Buildings;
using Assets.Scripts.Configs;
using Assets.Scripts.GameLogic;
using Assets.Scripts.GameLogic.Models;
using Assets.Scripts.HUD;
using Assets.Scripts.HUD.Menu;
using Assets.Scripts.PlayerAction;
using Assets.Scripts.Tiles;
using Assets.Scripts.Units;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.Utils;
using UnityEngine;
using Utils;
using Unit = Assets.Scripts.Units.Unit;

namespace Assets.Scripts.Controllers
{
    public class UIController : IService
    {
        private readonly Map _map;
        private readonly GridDrawer _gridDrawer;
        private readonly MenuController _menuController;
        private readonly TileInformationController _tileInfo;
        private readonly Camera _camera;
        private readonly HUDMessageController _hudMessageController;
        private readonly BalanceConfig _balanceConfig;
        private readonly MapActions _movement;

        private Vector2Int _hoveredGrid = new Vector2Int(-1, -1);
        private IGameplayObject _gameplayObject = null;

        private event Action<Vector3> _onMouseClick;
        private event Action<Vector3> _onMouseMove;
        private event Action<MouseController.DragData> _onMouseDrag;

        public UIController(Camera camera, ServiceLocator serviceLocator)
        {
            _map = serviceLocator.GetService<Map>();
            _gridDrawer = serviceLocator.GetService<GridDrawer>();
            _menuController = serviceLocator.GetService<MenuController>();
            _tileInfo = serviceLocator.GetService<TileInformationController>();
            _camera = camera;
            _hudMessageController = serviceLocator.GetService<HUDMessageController>();

            _balanceConfig = serviceLocator.GetService<BalanceConfig>();
            _movement = new MapActions(_map, _balanceConfig);

            _onMouseDrag += HideMenuOnDrag;
            _onMouseDrag += DisableHoverOnDrag;
            _onMouseMove += ShowCursorOnHover;

            _onMouseMove += UpdateTileInfo;
        }

        public async UniTask ShowMessage(string msg)
        {
            _hudMessageController.ShowMsg(msg);
            await UniTask.Delay(1000);
            _hudMessageController.HideMsg();
        }

        public async UniTask<IPlayerAction> GetPlayerAction(Player player)
        {
            try
            {
                Unit unit;
                Building building;

                _gameplayObject = await SelectGameplayObject(player);

                if (_gameplayObject == null || _gameplayObject.Owner != player)
                {
                    return await ProcessGameMatchAction();
                }

                if (_gameplayObject is Unit)
                {
                    unit = _gameplayObject as Unit;
                    building = null;

                    return await ProcessUnitPlayerAction(unit);
                }
                else if (_gameplayObject is Building)
                {
                    building = _gameplayObject as Building;

                    return await ProcessBuildingPlayerAction(building, player);
                }
            }
            catch (UserCanceledActionException)
            {
                _onMouseClick = null;
            }

            return null;
        }

        private async UniTask<IPlayerAction> ProcessGameMatchAction()
        {
            var action = await SelectGameMatchAction();

            return action switch
            {
                GameMatchAction.EndTurn => new EndTurn(),
                _ => throw new ArgumentOutOfRangeException($"{nameof(GameMatchAction)}: {action.ToString()}"),
            };
        }

        private async UniTask<IPlayerAction> ProcessUnitPlayerAction(Unit unit)
        {
            var coords = GetActionCoordsForUnit(unit).ToList();
            var hasHasActionCoords = coords.Count > 0;
            var coord = hasHasActionCoords ? await SelectCoord(coords, unit.Coord) : unit.Coord;
            var action = await SelectUnitAction(unit, coord);

            switch (action)
            {
                case UnitAction.Wait:
                    return new Wait
                    {
                        unit = unit
                    };
                    
                case UnitAction.Move:
                    return new MoveUnit
                    {
                        unit = unit,
                        coord = coord,
                    };
                case UnitAction.Attack:
                    {
                        var standingCoord = coords.First(i => i.coord == coord).moveInfo
                            .PathwayPart.Previous.CurrentMoveInfo.Coord;
                        var enemyUnit = _map[coord].Unit;

                        return new AttackUnit
                        {
                            StandingCoord = standingCoord,
                            Attacker = unit,
                            Defender = enemyUnit,
                        };
                    }
                case UnitAction.Occupy:
                    return new OccupyBuilding
                    {
                        Unit = unit,
                        Coord = coord,
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async UniTask<IPlayerAction> ProcessBuildingPlayerAction(Building building, Player player)
        {
            var coord = building.Coord;
            var unitType = await SelectBuildingAction(building, player);

            return unitType switch
            {
                UnitType.Soldier => new BuyUnit
                {
                    Unit = new Unit(
                        player,
                        building.Coord)
                },
                _ => throw new ArgumentOutOfRangeException($"{nameof(UnitType)}: {unitType.ToString()}"),
            };
        }

        GridType GetGridType(Unit currentUnit, Vector2Int coord)
        {
            if (_movement.HasEnemyUnit(currentUnit, coord))
            {
                return GridType.Enemy;
            }

            return GridType.Default;
        }

        private Task<Vector2Int> SelectCoord(IEnumerable<GridItem> gridItems, Vector2Int unitCoord)
        {
            var availableCoords = gridItems.ToDictionary(keySelector: i => i.coord);
            _gridDrawer.ShowGrid(availableCoords.Values);

            return ListenMouseClick<Vector2Int>((taskSource, pos) =>
            {
                _gridDrawer.Hide();
                var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);
                if (!availableCoords.ContainsKey(coord) && coord != unitCoord)
                {
                    taskSource.TrySetException(new UserCanceledActionException());
                }
                else
                {
                    taskSource.TrySetResult(coord);
                }
            });
        }

        private async Task<GameMatchAction> SelectGameMatchAction()
        {
            var taskSource = new UniTaskCompletionSource<GameMatchAction>();

            _menuController.ShowMenu(Input.mousePosition,
                GetGameMacthMenu(
                    onActionSelected: action => taskSource.TrySetResult(action)),
                onCancel: () => taskSource.TrySetException(new UserCanceledActionException()));

            return await taskSource.Task;
        }

        private async Task<UnitAction> SelectUnitAction(Unit unit, Vector2Int coord)
        {
            var taskSource = new UniTaskCompletionSource<UnitAction>();

            _menuController.ShowMenu(Input.mousePosition,
                GetUnitMenu(unit, coord,
                    onActionSelected: action => taskSource.TrySetResult(action)),
                onCancel: () => taskSource.TrySetException(new UserCanceledActionException()));

            return await taskSource.Task;
        }

        private async Task<UnitType> SelectBuildingAction(Building building, Player player)
        {
            var taskSource = new UniTaskCompletionSource<UnitType>();

            _menuController.ShowMenu(Input.mousePosition,
                GetBuyMenu(
                    building,
                    player,
                    onActionSelected: action => taskSource.TrySetResult(action)),
                onCancel: () => taskSource.TrySetException(new UserCanceledActionException()));

            return await taskSource.Task;
        }

        private void DisableHoverOnDrag(MouseController.DragData dragData)
        {
            switch (dragData.status)
            {
                case MouseController.DragStatus.Start:
                    _gridDrawer.HideCursor();
                    break;
                case MouseController.DragStatus.Stop:
                    _gridDrawer.ShowCursor(FieldUtils.GetCoordFromMousePos(dragData.currentPos, _camera));
                    break;
            }
        }

        private void HideMenuOnDrag(MouseController.DragData dragData)
        {
            if (dragData.status != MouseController.DragStatus.Start) return;
            _menuController.Hide();
        }

        private void ShowCursorOnHover(Vector3 pos)
        {
            var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);
            if (!_map.IsValidCoord(coord))
            {
                _gridDrawer.HideCursor();
                _hoveredGrid = coord;
                return;
            }

            if (_hoveredGrid == coord) return;

            _hoveredGrid = coord;
            _gridDrawer.ShowCursor(coord);
        }

        private void UpdateTileInfo(Vector3 pos)
        {
            var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);

            if (_map.IsValidCoord(coord))
            {
                _tileInfo.ShowInfoFor(coord);
            }
            else
            {
                _tileInfo.Hide();
            }
        }

        private Task<IGameplayObject> SelectGameplayObject(Player player)
        {
            return ListenMouseClick<IGameplayObject>((taskSource, pos) =>
            {
                var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);

                var isValidCoord = _map.IsValidCoord(coord);
                if (isValidCoord)
                {
                    var building = _map[coord].Building;

                    if (_map[coord].Unit != null)
                    {
                        taskSource.TrySetResult(_map[coord].Unit);
                    }
                    else if (building != null
                             && building.Type == BuildingType.Castle
                             && building.Owner == player)
                    {
                        taskSource.TrySetResult(_map[coord].Building);
                    }
                    else
                    {
                        taskSource.TrySetResult(null);
                    }
                }
                else
                {
                    taskSource.TrySetException(new UserCanceledActionException());
                }
            });
        }

        private async Task<T> ListenMouseClick<T>(Action<UniTaskCompletionSource<T>, Vector3> onMouseClick)
        {
            var taskSource = new UniTaskCompletionSource<T>();

            void OnMouseClickHandler(Vector3 pos)
            {
                onMouseClick.Invoke(taskSource, pos);
            }

            await UniTask.NextFrame(); // required to not process mouse clicks on the same frame

            _onMouseClick += OnMouseClickHandler;

            var result = await taskSource.Task;
            _onMouseClick -= OnMouseClickHandler;

            return result;
        }

        public void OnMouseDrag(MouseController.DragData dragData)
        {
            _onMouseDrag?.Invoke(dragData);
        }

        public void OnMouseMove(Vector3 position)
        {
            _onMouseMove?.Invoke(position);
        }

        public void OnMouseClick(Vector3 position)
        {
            _onMouseClick?.Invoke(position);
        }

        private IEnumerable<GridItem> GetActionCoordsForUnit(Unit unit) => _movement.GetPossibleActions(unit)
                .Select(cell => new GridItem()
                {
                    moveInfo = cell,
                    coord = cell.Coord,
                    type = GridTypeFromMoveInfoAttributes(cell),
                });

        private IEnumerable<MenuItem> GetGameMacthMenu(Action<GameMatchAction> onActionSelected)
        {
            yield return new MenuItem()
            {
                title = "End turn",
                onClick = () => onActionSelected.Invoke(GameMatchAction.EndTurn),
            };
        }

        private IEnumerable<MenuItem> GetUnitMenu(Unit unit, Vector2Int targetCoord,
            Action<UnitAction> onActionSelected)
        {
            if (!unit.IsEnabled)
            {
                yield break;
            }

            bool hasActions = true;
            if (!unit.HasPerformedAction)
            {
                if (_movement.HasEnemyUnit(unit, targetCoord))
                {
                    yield return new MenuItem()
                    {
                        title = "Attack",
                        onClick = () => onActionSelected.Invoke(UnitAction.Attack)
                    };
                }
                else if (_movement.HasEnemyBuilding(unit, targetCoord))
                {
                    yield return new MenuItem()
                    {
                        title = "Occupy",
                        onClick = () => onActionSelected.Invoke(UnitAction.Occupy)
                    };
                }
                else
                {
                    hasActions = false;
                }
            }
            else
            {
                hasActions = false;
            }

            if (targetCoord == unit.Coord) 
            {
                yield return new MenuItem()
                {
                    title = "Wait",
                    onClick = () => onActionSelected.Invoke(UnitAction.Wait)
                };
            }
            else if (!hasActions && !unit.HasMoved)
            {
                onActionSelected(UnitAction.Move);
            }
        }

        private IEnumerable<MenuItem> GetBuyMenu(Building building, Player player, Action<UnitType> onActionSelected)
        {
            if (_movement.HasEmptyCastle(building, player))
            {
                yield return new MenuItem()
                {
                    title = $"150 {UnitType.Soldier}",
                    onClick = () => onActionSelected.Invoke(UnitType.Soldier)
                };
            }
        }

        private GridType GridTypeFromMoveInfoAttributes(MoveInfo moveInfo)
        {
            if (moveInfo.CanAttack)
            {
                return GridType.Enemy;
            }

            if (moveInfo.CanOccupy)
            {
                return GridType.EnemyBuilding;
            }

            return GridType.Default;
        }

        private struct UIState
        {
            public bool isShowingMenu;
        }

        private enum UnitAction
        {
            Move,
            Attack,
            Occupy,
            BuyUnit,
            Wait
        }

        private enum GameMatchAction
        {
            EndTurn,
        }
    }
}
