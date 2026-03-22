using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public class CameraController : MonoBehaviour, IService
    {
        public float zoomSensitivity = 1f;
        public float zoomSmoothingTime = 0.3f;
        public Vector2 minMaxZoom;
        private MouseController _mouseController;
        private Transform _cameraTransform;
        private float _targetZoom;
        private float _currentZoomVelocity;

        [SerializeField] private ServiceLocator _serviceLocator;
        [SerializeField] private Camera _camera;
        
        private void Start()
        {
            _mouseController = _serviceLocator.GetService<MouseController>();
            _cameraTransform = _camera.transform;
            _targetZoom = _camera.orthographicSize;
            _mouseController.onDrag += OnDrag;
            _mouseController.onZoom += OnZoom;
        }

        private void OnDestroy()
        {
            _mouseController.onDrag -= OnDrag;
            _mouseController.onZoom -= OnZoom;
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
