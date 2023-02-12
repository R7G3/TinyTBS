using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class Pool<T> where T : Component
    {
        private readonly Func<T> _factory;
        private Queue<T> _items = new Queue<T>();

        public Pool(Func<T> factory)
        {
            _factory = factory;
        }

        public void WarmUp(int size)
        {
            for (int i = 0; i < size; i++)
            {
                _items.Enqueue(_factory.Invoke());
            }
        }

        public void Return(T item)
        {
            item.gameObject.SetActive(false);
            _items.Enqueue(item);
        }

        public T Get()
        {
            if (_items.Count > 0)
            {
                var item = _items.Dequeue();
                item.gameObject.SetActive(true);
                return item;
            }

            return _factory.Invoke();
        }
    }
}