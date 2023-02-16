using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

public class UnitView : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private float _travelSpeed = 1f;
    [SerializeField] private Transform _rootTransform;
    private Vector3 _velocity;
    private Vector3 _lastPos;

    public async Task Travel(IEnumerable<Vector2Int> path)
    {
        foreach (var coord in path)
        {
            var targetPoint = FieldUtils.GetWorldPos(coord);
            var threshold = 0.001f;
            var delta = targetPoint - _rootTransform.position;
            do
            {
                await UniTask.DelayFrame(1);
                var position = _rootTransform.position;
                position = Vector3.SmoothDamp(position, targetPoint, ref _velocity,
                    1f / _travelSpeed);
                _rootTransform.position = position;
                _rootTransform.rotation = Quaternion.LookRotation((targetPoint - position).normalized, Vector3.up);
                delta = targetPoint - _rootTransform.position;
            } while (delta.sqrMagnitude > threshold * threshold);
            _rootTransform.position = targetPoint;
        }
    }

    // private void Update()
    // {
    //     var currentPos = _rootTransform.position;
    //     var delta = _lastPos - currentPos;
    //     var angle = Vector3.SignedAngle(delta, Vector3.forward, Vector3.up);
    //     _rootTransform.rotation = Quaternion.Euler(0f, angle, 0f);
    //     _lastPos = currentPos;
    // }
}