using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

namespace Assets.Scripts.HUD
{
    public class GridDrawer : MonoBehaviour
    {
        public GameObject gridRectPrefab;
        private Transform _transform;
        private Pool<GridRect> _pool;
        private HashSet<Vector2Int> _shownCoords = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, GridRect> _activeRects = new();
        private GridRect _mouseOverRect;
        private GridRect _selectedRect;
        private int _hudLayerMask;

        public void ShowGrid(IEnumerable<Vector2Int> coords)
        {
            _shownCoords.Clear();
            _shownCoords.AddRange(coords);

            var coordsToRemove = _activeRects.Keys
                .Where(k => !_shownCoords.Contains(k)).ToList();

            foreach (var coord in coordsToRemove)
            {
                CleanUp(coord);
            }
            
            foreach (var coord in _shownCoords)
            {
                if (!_activeRects.TryGetValue(coord, out var rect))
                {
                    rect = _pool.Get();
                    rect.transform.localPosition = FieldUtils.GetWorldPos(coord);
                    _activeRects.Add(coord, rect);
                }
                
                rect.SetSelected(false);
                rect.SetMouseOver(false);
            }
        }

        public void Hide()
        {
            CleanUp();
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
            _hudLayerMask = LayerMask.GetMask("HUD");
        }

        private void CleanUp(Vector2Int coord)
        {
            var gridRect = _activeRects[coord];
            _pool.Return(gridRect);
            _activeRects.Remove(coord);
        }

        private void CleanUp()
        {
            foreach (var gridRect in _activeRects)
            {
                _pool.Return(gridRect.Value);
            }
            _activeRects.Clear();
        }

        private void RunMouseRaycastHit(Action<GridRect> action)
        {
            var mousePos = Input.mousePosition;
            var ray = Camera.main.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, _hudLayerMask))
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
                if (_mouseOverRect == gridRect) return;
                _mouseOverRect?.SetMouseOver(false);
                gridRect?.SetMouseOver(true);
                _mouseOverRect = gridRect;
            });
        }
    }
}