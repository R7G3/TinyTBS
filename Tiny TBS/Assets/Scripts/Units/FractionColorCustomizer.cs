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
        [SerializeField] private Color _disabledColor = Color.grey;

        private IGameplayObject _gameplayObject;
        private string _currentFraction;
        private bool _gameObjectIsEnabled;

        public void Init(IGameplayObject gameplayObject)
        {
            _gameplayObject = gameplayObject;
        }

        public void SetGameplayObjectEnabled(bool isEnabled)
        {
            if (_gameObjectIsEnabled == isEnabled)
            {
                return;
            }

            if (isEnabled)
            {
                UpdateFraction();
            }
            else
            {
                SetColor(_disabledColor);
            }

            _gameObjectIsEnabled = isEnabled;
        }

        private void Update()
        {
            if (_gameplayObject == null)
            {
                return;
            }

            if (_gameObjectIsEnabled != _gameplayObject.IsEnabled)
            {
                SetGameplayObjectEnabled(_gameplayObject.IsEnabled);
            }
            else if (_currentFraction != _gameplayObject.Owner.Id)
            {
                UpdateFraction();
            }
        }

        private void UpdateFraction()
        {
            var fractionConfig = _config.fractions.First(i => i.id == _gameplayObject.Owner.Id);
            SetColor(fractionConfig.color);
            _currentFraction = _gameplayObject.Owner.Id;
        }

        private void SetColor(Color color)
        {
            foreach (var rendererMaterial in _renderers)
            {
                 rendererMaterial.renderer.materials[rendererMaterial.materialIndex].color = color;
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
