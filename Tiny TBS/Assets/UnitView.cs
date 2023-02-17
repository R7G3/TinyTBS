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
    private int _speedParameterHash;
    private Vector3 _velocity;
    private Vector3 _lastPos;

    private void Awake()
    {
        _speedParameterHash = Animator.StringToHash("speed");
    }

    public async Task Travel(IEnumerable<Vector2Int> path)
    {
        const float animationThresholdDistance = 0.001f;
        foreach (var coord in path)
        {
            var targetPoint = FieldUtils.GetWorldPos(coord);
            bool hasReachedPoint;

            do
            {
                await UniTask.DelayFrame(1);
                
                var position = _rootTransform.position;
                var prevPosition = position;
                
                position = Vector3.SmoothDamp(position, targetPoint, ref _velocity,
                    1f / _travelSpeed);
                _rootTransform.position = position;
                _rootTransform.rotation = Quaternion.LookRotation((targetPoint - position).normalized, Vector3.up);
                
                var distanceToTargetPoint = targetPoint - position;
                
                var speed = (position - prevPosition).magnitude;
                _animator.SetFloat(_speedParameterHash, speed);
                
                hasReachedPoint = distanceToTargetPoint.sqrMagnitude <
                                  animationThresholdDistance * animationThresholdDistance;
            } while (!hasReachedPoint);
            
            _rootTransform.position = targetPoint; // make sure we at the final position after interpolation
        }
    }

}