using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.Units;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Assets.Scripts.Controllers
{
    public class UnitController : MonoBehaviour
    {
        [SerializeField] private GameObject _countLabelPrefab;
        [SerializeField] private Transform _countLabelsRoot;
        [SerializeField] private GameObject _unitPrefab;
        
        private readonly Dictionary<Guid, UnitView> _unitViews = new Dictionary<Guid, UnitView>();
        private Pool<Transform> _countLabelsPool;

        private void Awake()
        {
            _countLabelsPool = new Pool<Transform>(CreateCountLabel);
        }

        public void CreateUnitAt(Unit unit, Vector2Int coord)
        {
            _unitViews[unit.Id] = UnityEngine.Object.Instantiate(_unitPrefab)
                .GetComponent<UnitView>();
            var unitTransform = _unitViews[unit.Id].gameObject.transform;
            unitTransform.position = FieldUtils.GetWorldPos(coord);
            _countLabelsPool.Get().GetComponent<FollowUnitPosition>().FollowUnit(unitTransform);
        }

        public UniTask MoveUnit(Unit unit, IEnumerable<Vector2Int> path)
        {
            return _unitViews[unit.Id].Travel(path);
        }

        private Transform CreateCountLabel()
        {
            return UnityEngine.Object.Instantiate(_countLabelPrefab, _countLabelsRoot).transform;
        }
    }
}