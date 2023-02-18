using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.Units;
using UnityEngine;
using Utils;

namespace Assets.Scripts.Controllers
{
    public class UnitController
    {
        private readonly Dictionary<Guid, UnitView> _unitViews = new Dictionary<Guid, UnitView>();
        private readonly GameObject _unitPrefab;
        
        public UnitController(GameObject unitPrefab)
        {
            _unitPrefab = unitPrefab;
        }

        public void CreateUnitAt(Unit unit, Vector2Int coord)
        {
            _unitViews[unit.Id] = UnityEngine.Object.Instantiate(_unitPrefab)
                .GetComponent<UnitView>();
            _unitViews[unit.Id].gameObject.transform.position = FieldUtils.GetWorldPos(coord);
        }

        public Task MoveUnit(Unit unit, IEnumerable<Vector2Int> path)
        {
            return _unitViews[unit.Id].Travel(path);
        }
    }
}