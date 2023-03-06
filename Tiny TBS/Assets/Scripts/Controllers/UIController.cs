using Assets.Scripts.Buildings;
using Assets.Scripts.Configs;
using Assets.Scripts.GameLogic;
using Assets.Scripts.GameLogic.Models;
using Assets.Scripts.HUD;
using Assets.Scripts.HUD.Menu;
using Assets.Scripts.PlayerAction;
using Assets.Scripts.Tiles;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Utils;
using Unit = Assets.Scripts.Units.Unit;

namespace Assets.Scripts.Controllers
{
    public class UIController
    {
        private readonly Map _map;
        private readonly GridDrawer _gridDrawer;
        private readonly MenuController _menuController;
        private readonly TileInformationVisibilityController _widgetVisibility;
        private readonly TileInfoController _terrainInfo;
        private readonly TileInfoController _buildInfo;
        private readonly TileInfoController _unitInfo;
        private readonly Camera _camera;
        private readonly HUDMessageController _hudMessageController;
        private Unit _selectedUnit;
        private Vector2Int _selectedCoord;
        private Action _selectedAction;
        private Vector2Int _hoveredGrid = new Vector2Int(-1, -1);
        private Task _currentScenarioTask;
        private IGameplayObject gameplayObject = null;

        private readonly BalanceConfig _balanceConfig;
        private readonly MapActions _movement;

        private event Action<Vector3> _onMouseClick;
        private event Action<Vector3> _onMouseMove;
        private event Action<MouseController.DragData> _onMouseDrag;

        public UIController(Map map, GridDrawer gridDrawer, MenuController menuController, Camera camera,
            HUDMessageController hudMessageController, BalanceConfig balanceConfig,
            TileInfoController terrainInfo, TileInfoController buildInfo, TileInfoController unitInfo,
            TileInformationVisibilityController widgetVisibility)
        {
            _map = map;
            _gridDrawer = gridDrawer;
            _menuController = menuController;
            _widgetVisibility = widgetVisibility;
            _terrainInfo = terrainInfo;
            _buildInfo = buildInfo;
            _unitInfo = unitInfo;
            _camera = camera;
            _hudMessageController = hudMessageController;

            _balanceConfig = balanceConfig;
            _movement = new MapActions(_map, _balanceConfig);

            _onMouseDrag += HideMenuOnDrag;
            _onMouseDrag += DisableHoverOnDrag;
            _onMouseMove += ShowCursorOnHover;

            _onMouseMove += GetTileInfo;
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
                do
                {
                    gameplayObject = await SelectGameplayObject(player);
                } while (gameplayObject == null || gameplayObject.Fraction != player.Fraction);

                if (gameplayObject is Unit)
                {
                    unit = gameplayObject as Unit;
                    building = null;

                    return await ProcessUnitPlayerAction(unit);
                }
                else if (gameplayObject is Building)
                {
                    building = gameplayObject as Building;

                    return await ProcessBuildingPlayerAction(building);
                }
            }
            catch (UserCanceledActionException)
            {
                _onMouseClick = null;
            }

            return null;
        }

        private async UniTask<IPlayerAction> ProcessUnitPlayerAction(Unit unit)
        {
            Building building = null;
            var coords = GetActionCoordsForUnit(unit);
            var coord = await SelectCoord(coords);
            var action = await SelectAction(unit, building, coord);

            switch (action)
            {
                case Action.Move:
                    return new MoveUnit
                    {
                        unit = unit,
                        coord = coord,
                    };
                case Action.Attack:
                    {
                        var enemyUnit = _map[coord].Unit;

                        return new AttackUnit
                        {
                            Attacker = unit,
                            Defender = enemyUnit,
                        };
                    }
                case Action.Occupy:
                    return new OccupyBuilding
                    {
                        Unit = unit,
                        Coord = coord,
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async UniTask<IPlayerAction> ProcessBuildingPlayerAction(Building building)
        {
            Unit unit = null;
            var coord = building.Coord;
            var action = await SelectAction(unit, building, coord);

            switch (action)
            {
                case Action.BuyUnit:
                    return new BuyUnit
                    {
                        unit = new Unit(
                        building.Fraction,
                        building.Coord)
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        GridType GetGridType(Unit currentUnit, Vector2Int coord)
        {
            if (_movement.HasEnemyUnit(currentUnit, coord))
            {
                return GridType.Enemy;
            }
            
            return GridType.Default;
        }

        private Task<Vector2Int> SelectCoord(IEnumerable<GridItem> gridItems)
        {
            var availableCoords = gridItems.ToDictionary(keySelector: i => i.coord);
            _gridDrawer.ShowGrid(availableCoords.Values);

            return ListenMouseClick<Vector2Int>((taskSource, pos) =>
            {
                _gridDrawer.Hide();
                var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);
                if (!availableCoords.ContainsKey(coord))
                {
                    taskSource.TrySetException(new UserCanceledActionException());
                }
                else
                {
                    taskSource.TrySetResult(coord);
                }
            });
        }

        private async Task<Action> SelectAction(Unit unit, Building building, Vector2Int coord)
        {
            var taskSource = new UniTaskCompletionSource<Action>();

            _menuController.ShowMenu(Input.mousePosition,
                GetUnitMenu(unit, building, coord,
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

        private void GetTileInfo(Vector3 pos)
        {
            var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);

            if (_map.IsValidCoord(coord))
            {
                var tileInfo = TileInformation.GetTileInfo(coord, _map, _balanceConfig, InfoType.Tile);
                var buildingInfo = TileInformation.GetTileInfo(coord, _map, _balanceConfig, InfoType.Building);
                var unitInfo = TileInformation.GetTileInfo(coord, _map, _balanceConfig, InfoType.Unit);

                _widgetVisibility.ChangeVisibility(coord, _map);

                _terrainInfo.SetTileInfo(tileInfo);
                _unitInfo.SetTileInfo(unitInfo);
                _buildInfo.SetTileInfo(buildingInfo);
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
                            && building.Fraction == player.Fraction)
                    {
                        taskSource.TrySetResult(_map[coord].Building);
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

        private IEnumerable<GridItem> GetActionCoordsForUnit(Unit unit)
        {
            var possibleActions = _movement.GetPossibleActions(unit)
                .Select(cell => new GridItem()
                {
                    coord = cell.Coord,
                    type = GridTypeFromMoveInfoAttributes(cell),
                });

            return possibleActions;
        }

        private IEnumerable<MenuItem> GetUnitMenu(Unit unit, Building building, Vector2Int targetCoord, Action<Action> onActionSelected)
        {
            if (_movement.HasEnemyUnit(unit, targetCoord))
            {
                yield return new MenuItem()
                {
                    title = "Attack",
                    onClick = () => onActionSelected.Invoke(Action.Attack)
                };
            }
            else if (_movement.HasEnemyBuilding(unit, targetCoord))
            {
                yield return new MenuItem()
                {
                    title = "Occupy",
                    onClick = () => onActionSelected.Invoke(Action.Occupy)
                };
            }
            else if (_movement.HasEmptyCastle(building, targetCoord))
            {
                yield return new MenuItem()
                {
                    title = "Buy swordMan",
                    onClick = () => onActionSelected.Invoke(Action.BuyUnit)
                };
            }
            else
            {
                onActionSelected(Action.Move);
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

        private enum Action
        {
            Move,
            Attack,
            Occupy,
            BuyUnit
        }
    }
}