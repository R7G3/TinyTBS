using System.Linq;
using Assets.Scripts.Configs;
using UnityEngine;

namespace Assets.Scripts.Units
{
    public class FractionColorCustomizer : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private FractionsConfig _config;

        public void Init(IFraction fraction)
        {
            var fractionConfig = _config.fractions.First(i => i.id == fraction.Id);
            foreach (var renderer in _renderers)
            {
                 renderer.material.color = fractionConfig.color;
            }
        }
    }
}