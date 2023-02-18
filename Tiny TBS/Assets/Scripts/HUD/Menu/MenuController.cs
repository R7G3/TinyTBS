using System;
using System.Collections.Generic;
using Assets.Scripts.HUD;
using UnityEngine;
using Utils;

namespace Assets.Scripts.HUD.Menu
{
    public struct MenuItem
    {
        public string title;
        public Action onClick;
    }

    public interface IHUDMenu
    {
        void ShowMenu(Vector3 screenPosition, IEnumerable<MenuItem> menuItems);
        void Hide();
    }
    
    public class MenuController : MonoBehaviour, IHUDMenu
    {
        [SerializeField] private RectTransform _list;
        [SerializeField] private MenuItemView _menuItemPrefab;

        private Pool<MenuItemView> _pool;

        private void Awake()
        {
            _pool = new Pool<MenuItemView>(CreateMenuItem);
            _pool.WarmUp(5);
        }

        private void Start()
        {
            _list.gameObject.SetActive(false);
        }

        public void ShowMenu(Vector3 screenPosition, IEnumerable<MenuItem> menuItems)
        {
            _list.gameObject.SetActive(true);
            Clear();
            foreach (var item in menuItems)
            {
                _pool.Get().Init(item.title, () =>
                {
                    item.onClick.Invoke();
                    Hide();
                });
            }

            _list.position = screenPosition;
        }

        public void Hide()
        {
            _list.gameObject.SetActive(false);
        }

        private void Clear()
        {
            for (int i = 0; i < _list.childCount; i++)
            {
                _pool.Return(_list.GetChild(i).GetComponent<MenuItemView>());
            }
        }

        private MenuItemView CreateMenuItem()
        {
            return Instantiate(_menuItemPrefab, _list);
        }
    }
}