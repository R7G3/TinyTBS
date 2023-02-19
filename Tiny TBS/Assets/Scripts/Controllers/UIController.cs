using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Assets.Scripts.HUD;
using Assets.Scripts.HUD.Menu;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;
using Unit = Assets.Scripts.Units.Unit;

namespace Assets.Scripts.Controllers
{
    public class UIController
    {
        private readonly Map _map;
        private readonly GridDrawer _gridDrawer;
        private readonly MenuController _menuController;
        private readonly Camera _camera;
        private Unit _selectedUnit;
        private Vector2Int _selectedCoord;
        private UnitAction _selectedAction;
        private Task _currentScenarioTask;

        public event Action<Unit, Vector2Int> onMoveUnit;
        private event Action<Vector3> _onMouseClick;
        private event Action<Vector3> _onMouseMove;
        private event Action<MouseController.DragData> _onMouseDrag;

        public UIController(Map map, GridDrawer gridDrawer, MenuController menuController, Camera camera)
        {
            _map = map;
            _gridDrawer = gridDrawer;
            _menuController = menuController;
            _camera = camera;

            _onMouseDrag += HideMenuOnDrag;
        }

        public async Task StartIdleScenario()
        {
            try
            {
                Unit unit;
                do
                {
                    unit = await SelectUnit();
                } while (unit == null);

                var action = await SelectAction(unit);
                switch (action)
                {
                    case UnitAction.Move:
                        var coords = GetMoveCoordsForUnit(unit);
                        var coord = await SelectCoord(coords);
                        onMoveUnit?.Invoke(unit, coord);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (UserCanceledActionException)
            {
                _onMouseClick = null;
            }
        }

        private Task<Vector2Int> SelectCoord(IEnumerable<Vector2Int> coords)
        {
            var availableCoords = LinqUtility.ToHashSet(coords);
            _gridDrawer.ShowGrid(availableCoords);
            
            return ListenMouseClick<Vector2Int>(((taskSource, pos) =>
            {
                _gridDrawer.Hide();
                var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);
                if (!availableCoords.Contains(coord))
                {
                    taskSource.TrySetException(new UserCanceledActionException());
                }
                else
                {
                    taskSource.TrySetResult(coord);
                }
            }));
        }

        private async Task<UnitAction> SelectAction(Unit unit)
        {
            var taskSource = new UniTaskCompletionSource<UnitAction>();
            
            _menuController.ShowMenu(Input.mousePosition,
                GetUnitMenu(action => taskSource.TrySetResult(action)));

            void MouseClickHandler(Vector3 _)
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                {
                    _menuController.Hide();
                    _onMouseClick -= MouseClickHandler;
                    taskSource.TrySetException(new UserCanceledActionException());
                }
            }

            _onMouseClick += MouseClickHandler;
            
            return await taskSource.Task;
        }

        private void DisableHoverOnDrag(MouseController.DragData dragData)
        {
            switch (dragData.status)
            {
                case MouseController.DragStatus.Start:
                    _gridDrawer.Hide();
                    _onMouseMove -= ShowGridOnHover;
                    break;
                case MouseController.DragStatus.Stop:
                    _onMouseMove += ShowGridOnHover;
                    break;
            }
        }

        private void HideMenuOnDrag(MouseController.DragData dragData)
        {
            if (dragData.status != MouseController.DragStatus.Start) return;
            _menuController.Hide();
        }

        private void ShowGridOnHover(Vector3 pos)
        {
            var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);
            if (!_map.IsValidCoord(coord))
            {
                _gridDrawer.Hide();
                return;
            }
            _gridDrawer.ShowGrid(Enumerable.Repeat(coord, 1));
        }

        private Task<Unit> SelectUnit()
        {
            _onMouseMove += ShowGridOnHover;
            _onMouseDrag += DisableHoverOnDrag;
                
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

                _onMouseDrag -= DisableHoverOnDrag;
                _onMouseMove -= ShowGridOnHover;
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

        private IEnumerable<Vector2Int> GetMoveCoordsForUnit(Unit unit)
        {
            //return FieldUtils.GetNeighbours(unit.Coord);
            return _map.GetPossibleMoves(unit);
        }

        private IEnumerable<MenuItem> GetUnitMenu(Action<UnitAction> onUnitActionSelected)
        {
            yield return new MenuItem()
            {
                title = "Move",
                onClick = () => onUnitActionSelected.Invoke(UnitAction.Move)
            };
        }

        private enum UnitAction
        {
            Move
        }

        private class UserCanceledActionException : Exception
        {
        }
    }
}