using Assets.Scripts.GameLogic.Models;
using Assets.Scripts.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

namespace Assets.Scripts.HUD
{
    public class GridDrawer : MonoBehaviour, IService
    {
        public GameObject gridRectPrefab;
        public GameObject cursorPrefab;
        private Transform _transform;
        private Pool<GridRect> _pool;
        private readonly Dictionary<Vector2Int, GridItem> _shownItems = new();
        private readonly Dictionary<Vector2Int, GridRect> _activeRects = new();
        private GridRect _mouseOverRect;
        private GridRect _selectedRect;
        private int _hudLayerMask;
        private Transform _cursor;

        public void HideCursor()
        {
            if (_cursor == null) return;
            _cursor.gameObject.SetActive(false);
        }
        
        public void ShowCursor(Vector2Int coord)
        {
            if (_cursor == null)
            {
                _cursor = Instantiate(cursorPrefab, _transform, worldPositionStays: true).transform;
            }

            _cursor.gameObject.SetActive(true);
            _cursor.position = FieldUtils.GetWorldPos(coord);
        }

        public void ShowGrid(IEnumerable<GridItem> gridItems)
        {
            _shownItems.Clear();
            
            foreach (var item in gridItems)
            {
                _shownItems.Add(item.coord, item);
            }

            var coordsToRemove = _activeRects.Keys
                .Where(k => !_shownItems.ContainsKey(k)).ToList();

            foreach (var coord in coordsToRemove)
            {
                CleanUp(coord);
            }
            
            foreach (var kv in _shownItems)
            {
                if (!_activeRects.TryGetValue(kv.Key, out var rect))
                {
                    rect = _pool.Get();
                    rect.transform.localPosition = FieldUtils.GetWorldPos(kv.Key);
                    _activeRects.Add(kv.Key, rect);
                }
                
                rect.SetType(kv.Value.type);
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
    }

    public struct GridItem
    {
        public MoveInfo moveInfo;
        public Vector2Int coord;
        public GridType type;
    }

    public enum GridType
    {
        Default,
        AvailableForAttack,
        Enemy,
        EnemyBuilding,
    }
}
