using System;
using UnityEngine;

namespace Assets.Scripts.Input
{
    public class MouseController : MonoBehaviour
    {
        public event Action<Vector3> onClick;
        public event Action<DragData> onDrag;
        public event Action<float> onZoom;
        
        private bool _hasStartedClickBefore;
        private Vector3 _mouseCurrentPos;
        private Vector3 _clickStartPos;
        private Vector3 _mouseLastPos;
        
        public void Update()
        {
            var scrollDelta = UnityEngine.Input.mouseScrollDelta;
            if (scrollDelta != Vector2.zero)
            {
                onZoom?.Invoke(scrollDelta.y);
            }
            if (!UnityEngine.Input.GetMouseButton(0))
            {
                if (_hasStartedClickBefore)
                {
                    var dragDelta = _mouseLastPos - _clickStartPos;
                    var isDragging = dragDelta.sqrMagnitude > SqrDragThreshold;
                    if (!isDragging)
                    {
                        onClick?.Invoke(UnityEngine.Input.mousePosition);
                    }

                    _hasStartedClickBefore = false;
                }
                return;
            }

            if (!_hasStartedClickBefore)
            {
                var mousePos = UnityEngine.Input.mousePosition;
                _clickStartPos = mousePos;
                _mouseCurrentPos = mousePos;
                _mouseLastPos = mousePos;
                _hasStartedClickBefore = true;
            }
            else
            {
                _mouseLastPos = _mouseCurrentPos;
                _mouseCurrentPos = UnityEngine.Input.mousePosition;
                onDrag?.Invoke(new DragData()
                {
                    currentPos = _mouseCurrentPos,
                    lastPos = _mouseLastPos
                });
            }

        }

        public struct DragData
        {
            public Vector3 currentPos;
            public Vector3 lastPos;
        }
        
        private const float SqrDragThreshold = 10f;
        
    }
}