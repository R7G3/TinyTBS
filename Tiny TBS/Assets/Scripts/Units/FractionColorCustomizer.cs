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
        private IGameplayObject _gameplayObject;
        private string _currentFraction;

        public void Init(IGameplayObject gameplayObject)
        {
            _gameplayObject = gameplayObject;
        }

        private void Update()
        {
            if (_gameplayObject != null && _currentFraction != _gameplayObject.Owner.Id)
            {
                UpdateFraction();
            }
        }

        private void UpdateFraction()
        {
            var fractionConfig = _config.fractions.First(i => i.id == _gameplayObject.Owner.Id);
            foreach (var rendererMaterial in _renderers)
            {
                 rendererMaterial.renderer.materials[rendererMaterial.materialIndex].color = fractionConfig.color;
            }

            _currentFraction = _gameplayObject.Owner.Id;
        }

        [Serializable]
        public struct RendererMaterial
        {
            public Renderer renderer;
            public int materialIndex;
        }
    }
}