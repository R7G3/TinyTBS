using System;
using System.Linq;
using Assets.Scripts.Configs;
using UnityEngine;

namespace Assets.Scripts.Units
{
    public class FractionColorCustomizer : MonoBehaviour
    {
        [SerializeField] private RendererMaterial[] _renderers;
        [SerializeField] private FractionsConfig _config;

        public void Init(IFraction fraction)
        {
            var fractionConfig = _config.fractions.First(i => i.id == fraction.Id);
            foreach (var rendererMaterial in _renderers)
            {
                 rendererMaterial.renderer.materials[rendererMaterial.materialIndex].color = fractionConfig.color;
            }
        }

        [Serializable]
        public struct RendererMaterial
        {
            public Renderer renderer;
            public int materialIndex;
        }
    }
}