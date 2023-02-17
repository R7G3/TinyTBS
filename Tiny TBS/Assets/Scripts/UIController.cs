using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HUD.Menu;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

namespace Assets.Scripts
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

        public UIController(Map map, GridDrawer gridDrawer, MenuController menuController, Camera camera)
        {
            _map = map;
            _gridDrawer = gridDrawer;
            _menuController = menuController;
            _camera = camera;
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
            var availableCoords = coords.ToHashSet();
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
            
            _menuController.ShowMenu(UnityEngine.Input.mousePosition,
                GetUnitMenu(action => taskSource.TrySetResult(action)));

            void MouseClickHandler(Vector3 _)
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                {
                    _menuController.Hide();
                    taskSource.TrySetException(new UserCanceledActionException());
                }
            }

            _onMouseClick += MouseClickHandler;
            
            var result = await taskSource.Task;

            _onMouseClick -= MouseClickHandler;

            return result;
        }

        private Task<Unit> SelectUnit()
        {
            return ListenMouseClick<Unit>((taskSource, pos) =>
            {
                var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);

                // TODO: add check for map size
                var isValidCoord = coord.x >= 0 || coord.y >= 0;
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

        public void OnMouseClick(Vector3 position)
        {
            _onMouseClick?.Invoke(position);
        }

        private IEnumerable<Vector2Int> GetMoveCoordsForUnit(Unit unit) => FieldUtils.GetNeighbours(unit.Coord);

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