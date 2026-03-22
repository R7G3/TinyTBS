using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utils
{

    public interface IService {}

    /// <summary>
    /// source: https://habr.com/en/companies/otus/articles/725308/
    /// </summary>
    public sealed class ServiceLocator : MonoBehaviour
    {
        private readonly List<IService> _services = new();
        private readonly List<object> _lazy = new();

        public void AddService(IService service)
        {
            _services.Add(service);
        }
        
        public void Register<T>(Func<T> factory) where T: IService
        {
            _lazy.Add(new Lazy<T>(factory));
        }


        public void RemoveService(IService service)
        {
            _services.Remove(service);
            _lazy.RemoveAll(i =>
                {
                    var lazyService = i as Lazy<IService>;
                    return lazyService?.IsValueCreated == true && lazyService.Value == service;
                }
            );
        }

        public T GetService<T>() where T: IService
        {
            foreach (var service in _services)
            {
                if (service is T result)
                {
                    return result;
                }
            }
            
            foreach (var service in _lazy)
            {
                if (service is Lazy<T> result)
                {
                    return result.Value;
                }
            }

            throw new Exception($"Service of type {typeof(T)} is not found!");
        }
    }
}
