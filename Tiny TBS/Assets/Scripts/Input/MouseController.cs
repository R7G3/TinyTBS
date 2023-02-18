using System;
using UnityEngine;

namespace Assets.Scripts.Input
{
    public class MouseController : MonoBehaviour
    {
        public event Action<Vector3> onClick;
        public event Action<DragData> onDrag;
        public event Action<float> onZoom;

        public event Action<Vector3> onMouseMove;

        private bool _isClickStarted;
        private Vector3 _mouseCurrentPos;
        private Vector3 _clickStartPos;
        private Vector3 _mouseLastPos;

        private bool HasStartedDragging()
        {
            var dragDelta = _mouseLastPos - _clickStartPos;
            return dragDelta.sqrMagnitude > SqrDragThreshold;
        }

        public void Update()
        {
            HandleHover();
            HandleZoom();

            if (!UnityEngine.Input.GetMouseButton(0))
            {
                HandleMouseUp();
                return;
            }

            if (!_isClickStarted) HandleMouseClickStart();
            HandleMouseMove();
        }

        private void HandleMouseClickStart()
        {
            var mousePos = UnityEngine.Input.mousePosition;
            _clickStartPos = mousePos;
            _mouseCurrentPos = mousePos;
            _mouseLastPos = mousePos;
            _isClickStarted = true;
        }

        /// <returns>true if click happened</returns>
        private void HandleMouseUp()
        {
            if (!_isClickStarted) return;

            if (!HasStartedDragging())
            {
                onClick?.Invoke(UnityEngine.Input.mousePosition);
            }

            _isClickStarted = false;
        }

        private void HandleMouseMove()
        {
            if (HasStartedDragging())
            {
                onDrag?.Invoke(new DragData()
                {
                    currentPos = _mouseCurrentPos,
                    lastPos = _mouseLastPos
                });
            }

            _mouseLastPos = _mouseCurrentPos;
            _mouseCurrentPos = UnityEngine.Input.mousePosition;
        }

        private void HandleZoom()
        {
            var scrollDelta = UnityEngine.Input.mouseScrollDelta;
            if (scrollDelta != Vector2.zero)
            {
                onZoom?.Invoke(scrollDelta.y);
            }
        }

        private void HandleHover()
        {
            if (_mouseLastPos != UnityEngine.Input.mousePosition)
            {
                onMouseMove?.Invoke(UnityEngine.Input.mousePosition);
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