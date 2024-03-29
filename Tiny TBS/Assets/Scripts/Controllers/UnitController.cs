using System;
using System.Collections.Generic;
using Assets.Scripts.HUD;
using Assets.Scripts.Units;
using Assets.Scripts.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Assets.Scripts.Controllers
{
    public class UnitController : MonoBehaviour, IService
    {
        [SerializeField] private GameObject _countLabelPrefab;
        [SerializeField] private Transform _countLabelsRoot;
        [SerializeField] private GameObject _unitPrefab;
        
        private readonly Dictionary<Guid, UnitView> _unitViews = new Dictionary<Guid, UnitView>();
        private readonly Dictionary<Unit, Transform> _unitLabelMap = new Dictionary<Unit, Transform>();
        private Pool<Transform> _countLabelsPool;

        private void Awake()
        {
            _countLabelsPool = new Pool<Transform>(CreateCountLabel);
        }

        public void CreateUnitAt(Unit unit, Vector2Int coord)
        {
            _unitViews[unit.Id] = UnityEngine.Object.Instantiate(_unitPrefab)
                .GetComponent<UnitView>();
            _unitViews[unit.Id].Init(unit);
            var unitTransform = _unitViews[unit.Id].gameObject.transform;
            unitTransform.position = FieldUtils.GetWorldPos(coord);

            var labelTransform = _countLabelsPool.Get();
            _unitLabelMap.Add(unit, labelTransform);
            labelTransform.GetComponent<FollowUnitPosition>().FollowUnit(unitTransform);
            labelTransform.GetComponent<UnitHealthUpdate>().Init(unit);
        }

        public void RemoveUnitAt(Unit unit)
        {
            UnityEngine.Object.Destroy(
                _unitViews[unit.Id].gameObject);

            _countLabelsPool.Return(_unitLabelMap[unit]);

            _unitViews.Remove(unit.Id);
        }
        
        public UniTask Attack(Unit unit, Vector2Int coord)
        {
            return _unitViews[unit.Id].Attack(coord);
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
