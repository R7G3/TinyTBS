using UnityEngine;

namespace Assets.Scripts.Utils
{
    public sealed class GameScopeServices : MonoBehaviour
    {
        [SerializeField]
        private ServiceLocator _locator;

        [SerializeField]
        private Object[] _objectsWithServices;

        private void Awake()
        {
            foreach (var obj in _objectsWithServices)
            {
                if (obj is IService objService)
                {
                    _locator.AddService(objService);
                }

                if (obj is GameObject go)
                {
                    foreach (var service in go.GetComponents<IService>())
                    {
                        _locator.AddService(service);
                    }
                }

            }
        }
    }
}

