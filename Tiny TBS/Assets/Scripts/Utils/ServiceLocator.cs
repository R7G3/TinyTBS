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
        private readonly List<IService> services = new();

        public void AddService(IService service)
        {
            this.services.Add(service);
        }

        public void RemoveService(IService service)
        {
            this.services.Remove(service);
        }

        public T GetService<T>() where T: IService
        {
            foreach (var service in this.services)
            {
                if (service is T result)
                {
                    return result;
                }
            }

            throw new Exception($"Service of type {typeof(T)} is not found!");
        }
    }
}
