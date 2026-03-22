using Assets.Scripts.Units;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.HUD
{
    public class UnitHealthUpdate : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        private Unit _unit;
        private string _unitHealth;

        public void Init(Unit unit)
        {
            _unit = unit;
        }

        private void Update()
        {
            if (_unit != null)
            {
                UpdateHealth();
            }
        }

        private void UpdateHealth()
        {
            _text.text = _unit.Health.ToString();
        }
    }
}