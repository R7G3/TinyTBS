using Assets.Scripts.Units;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.HUD
{
    public class UnitHealthUpdate : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        private string _unitHealth;

        public void SetUnitHealth(Unit unit)
        {
            _unitHealth = unit.Health.ToString();
        }

        private void Update()
        {
            _text.text = _unitHealth;
        }
    }
}