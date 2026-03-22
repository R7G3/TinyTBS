using UnityEngine;

namespace Assets.Scripts.HUD
{
    public class FollowUnitPosition : MonoBehaviour
    {
        private RectTransform _transform;
        private Transform _unitTransform;
        [SerializeField] private Vector3 _offset;

        public void FollowUnit(Transform unit)
        {
            _transform = GetComponent<RectTransform>();
            _unitTransform = unit;
        }

        private void Update()
        {
            _transform.position = _unitTransform.position + _offset;
        }
    }
}
