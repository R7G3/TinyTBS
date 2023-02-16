using System;
using Assets.Scripts.Input;
using UnityEngine;

namespace Assets.Scripts
{
    public class CameraController : MonoBehaviour
    {
        public float zoomSensitivity = 1f;
        public float zoomSmoothingTime = 0.3f;
        public Vector2 minMaxZoom;
        public MouseController mouseController;
        private Camera _camera;
        private Transform _cameraTransform;
        private float _targetZoom;
        private float _currentZoomVelocity;

        private void Awake()
        {
            _camera = Camera.main;
            _cameraTransform = _camera.transform;
            _targetZoom = _camera.orthographicSize;
            mouseController.onDrag += OnDrag;
            mouseController.onZoom += OnZoom;
        }

        private void OnDestroy()
        {
            mouseController.onDrag -= OnDrag;
            mouseController.onZoom -= OnZoom;
        }

        private void Update()
        {
            _camera.orthographicSize =
                Mathf.SmoothDamp(_camera.orthographicSize, _targetZoom, ref _currentZoomVelocity, zoomSmoothingTime);
        }

        private void OnZoom(float zoomDelta)
        {
            _targetZoom = Mathf.Clamp(_targetZoom + -zoomDelta * zoomSensitivity, minMaxZoom.x, minMaxZoom.y);
        }

        private void OnDrag(MouseController.DragData dragData)
        {
            if (!isActiveAndEnabled) return;
            var currentPos = _camera.ScreenToWorldPoint(dragData.currentPos);
            var lastPos = _camera.ScreenToWorldPoint(dragData.lastPos);
            _cameraTransform.position += -(currentPos - lastPos); // minus here for the natural scrolling
        }
    }
}