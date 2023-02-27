using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Assets.Scripts.HUD;
using Assets.Scripts.HUD.Menu;
using Assets.Scripts.PlayerAction;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;
using Unit = Assets.Scripts.Units.Unit;
using Assets.Scripts.GameLogic;
using Assets.Scripts.Configs;
using Assets.Scripts.GameLogic.Models;

namespace Assets.Scripts.Controllers
{
    public class UIController
    {
        private readonly Map _map;
        private readonly GridDrawer _gridDrawer;
        private readonly MenuController _menuController;
        private readonly TileInfoController _tileInfoController;
        private readonly Camera _camera;
        private readonly HUDMessageController _hudMessageController;
        private Unit _selectedUnit;
        private Vector2Int _selectedCoord;
        private UnitAction _selectedAction;
        private Vector2Int _hoveredGrid = new Vector2Int(-1, -1);
        private Task _currentScenarioTask;

        private readonly BalanceConfig _balanceConfig;
        private readonly MapActions _movement;

        private event Action<Vector3> _onMouseClick;
        private event Action<Vector3> _onMouseMove;
        private event Action<MouseController.DragData> _onMouseDrag;

        public UIController(Map map, GridDrawer gridDrawer, MenuController menuController, Camera camera,
            HUDMessageController hudMessageController, BalanceConfig balanceConfig, TileInfoController tileInfoController)
        {
            _map = map;
            _gridDrawer = gridDrawer;
            _menuController = menuController;
            _tileInfoController = tileInfoController;
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
                do
                {
                    unit = await SelectUnit();
                } while (unit == null || unit.Fraction != player.Fraction);

                var coords = GetActionCoordsForUnit(unit);
                var coord = await SelectCoord(coords);
                var action = await SelectAction(unit, coord);
                
                switch (action)
                {
                    case UnitAction.Move:
                        return new MoveUnit()
                        {
                            unit = unit,
                            coord = coord
                        };
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (UserCanceledActionException)
            {
                _onMouseClick = null;
            }

            return null;
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

        private async Task<UnitAction> SelectAction(Unit unit, Vector2Int coord)
        {
            var taskSource = new UniTaskCompletionSource<UnitAction>();

            _menuController.ShowMenu(Input.mousePosition,
                GetUnitMenu(unit, coord,
                    onUnitActionSelected: action => taskSource.TrySetResult(action)),
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
            string info = TileInformation.GetTileInfo(pos, _map, _balanceConfig, _camera, InfoType.Tile);

            _tileInfoController.SetTileInfo(info);
        }

        private Task<Unit> SelectUnit()
        {
            return ListenMouseClick<Unit>((taskSource, pos) =>
            {
                var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);

                var isValidCoord = _map.IsValidCoord(coord);
                if (isValidCoord)
                {
                    taskSource.TrySetResult(_map[coord].Unit);
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
                    type = cell.CanAttack ? GridType.Enemy : GridType.Default,
                });

            return possibleActions;
        }

        private IEnumerable<MenuItem> GetUnitMenu(Unit unit, Vector2Int targetCoord, Action<UnitAction> onUnitActionSelected)
        {
            if (_movement.HasEnemyUnit(unit, targetCoord))
            {
                yield return new MenuItem()
                {
                    title = "Attack",
                    onClick = () => onUnitActionSelected.Invoke(UnitAction.Attack)
                };
            }
            else
            {
                onUnitActionSelected(UnitAction.Move);
            }
        }

        private struct UIState
        {
            public bool isShowingMenu;
        }

        private enum UnitAction
        {
            Move,
            Attack
        }
    }
}