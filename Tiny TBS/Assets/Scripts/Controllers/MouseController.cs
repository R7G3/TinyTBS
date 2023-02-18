using System;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public class MouseController : MonoBehaviour
    {
        public event Action<Vector3> onClick;
        public event Action<DragData> onDrag;
        public event Action<float> onZoom;

        public event Action<Vector3> onMouseMove;

        private bool _isClickStarted;
        private bool _isDragStarted;
        private Vector3 _mouseCurrentPos;
        private Vector3 _clickStartPos;
        private Vector3 _mouseLastPos;

        private bool CanStartDrag()
        {
            var dragDelta = _mouseLastPos - _clickStartPos;
            return dragDelta.sqrMagnitude > SqrDragThreshold;
        }

        public void Update()
        {
            HandleHover();
            HandleZoom();

            if (!Input.GetMouseButton(0))
            {
                HandleMouseUp();
                return;
            }

            if (!_isClickStarted) HandleMouseClickStart();
            HandleMouseMove();
        }

        private void HandleMouseClickStart()
        {
            var mousePos = Input.mousePosition;
            _clickStartPos = mousePos;
            _mouseCurrentPos = mousePos;
            _mouseLastPos = mousePos;
            _isClickStarted = true;
        }

        /// <returns>true if click happened</returns>
        private void HandleMouseUp()
        {
            if (!_isClickStarted) return;

            if (!_isDragStarted)
            {
                onClick?.Invoke(Input.mousePosition);
            }
            else
            {
                onDrag?.Invoke(GetDragDataWithStatus(DragStatus.Stop));
                _isDragStarted = false;
            }

            _isClickStarted = false;
        }

        private DragData GetDragDataWithStatus(DragStatus status) => new ()
        {
            status = status,
            currentPos = _mouseCurrentPos,
            lastPos = _mouseLastPos
        };

        private void HandleMouseMove()
        {
            if (!_isDragStarted)
            {
                if (CanStartDrag())
                {
                    _isDragStarted = true;
                    onDrag?.Invoke(GetDragDataWithStatus(DragStatus.Start));
                }
            }
            if (_isDragStarted)
            {
                onDrag?.Invoke(GetDragDataWithStatus(DragStatus.InProgress));
            }

            _mouseLastPos = _mouseCurrentPos;
            _mouseCurrentPos = Input.mousePosition;
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
            if (_mouseLastPos != Input.mousePosition)
            {
                onMouseMove?.Invoke(Input.mousePosition);
            }
        }

        public struct DragData
        {
            public DragStatus status;
            public Vector3 currentPos;
            public Vector3 lastPos;
        }

        public enum DragStatus
        {
            Start,
            InProgress,
            Stop
        }

        private const float SqrDragThreshold = 10f;
    }
}