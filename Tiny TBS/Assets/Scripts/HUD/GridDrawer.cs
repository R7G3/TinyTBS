using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Utils;

namespace Assets.Scripts.HUD
{
    public class GridDrawer : MonoBehaviour
    {
        public GameObject gridRectPrefab;
        private Transform _transform;
        private Pool<GridRect> _pool;
        private readonly List<GridRect> _activeRects = new();
        private GridRect _mouseOverRect;
        private GridRect _selectedRect;

        public void ShowGrid(IEnumerable<Vector2Int> coords)
        {
            CleanUp();
            foreach (var coord in coords)
            {
                var rect = _pool.Get();
                rect.SetSelected(false);
                rect.SetMouseOver(false);
                rect.transform.localPosition = FieldUtils.GetWorldPos(coord);
                _activeRects.Add(rect);
            }
        }

        public void Hide()
        {
            CleanUp();
        }

        [SuppressMessage("ReSharper", "Unity.NoNullPropagation")]
        public void SelectGridRect(Vector3 mousePosition)
        {
            RunMouseRaycastHit(gridRect =>
            {
                _selectedRect?.SetSelected(false);
                gridRect?.SetSelected(true);
                _selectedRect = gridRect;
            });
        }

        private GridRect CreateGridRect()
        {
            return Instantiate(gridRectPrefab, _transform, worldPositionStays: true).GetComponent<GridRect>();
        }

        private void Awake()
        {
            _transform = GetComponent<Transform>();
            _pool = new Pool<GridRect>(CreateGridRect);
            _pool.WarmUp(10);
        }

        private void CleanUp()
        {
            foreach (var gridRect in _activeRects)
            {
                _pool.Return(gridRect);
            }
            _activeRects.Clear();
        }

        private void RunMouseRaycastHit(Action<GridRect> action)
        {
            var mousePos = Input.mousePosition;
            var ray = Camera.main.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out var hit))
            {
                action.Invoke(null);
                return;
            }
            action.Invoke(hit.transform.gameObject.GetComponentInParent<GridRect>());
        }

        [SuppressMessage("ReSharper", "Unity.NoNullPropagation")]
        private void Update()
        {
            RunMouseRaycastHit(gridRect =>
            {
                _mouseOverRect?.SetMouseOver(false);
                gridRect?.SetMouseOver(true);
                _mouseOverRect = gridRect;
            });
        }
    }
}