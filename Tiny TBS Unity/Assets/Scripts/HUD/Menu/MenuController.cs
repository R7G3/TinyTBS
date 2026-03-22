using System;
using System.Collections.Generic;
using Assets.Scripts.HUD;
using Assets.Scripts.Utils;
using UnityEngine;
using UnityEngine.UI;
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
        void ShowMenu(Vector3 screenPosition, IEnumerable<MenuItem> menuItems, Action onCancel = null);
        void Hide();
    }

    public class MenuController : MonoBehaviour, IHUDMenu, IService
    {
        [SerializeField] private RectTransform _list;
        [SerializeField] private MenuItemView _menuItemPrefab;
        [SerializeField] private Button _backgroundButton;
        private readonly List<MenuItem> _items = new();

        private Pool<MenuItemView> _pool;
        private Action _onCancel;

        private void Awake()
        {
            _pool = new Pool<MenuItemView>(CreateMenuItem);
            _pool.WarmUp(5);
            _backgroundButton.onClick.AddListener(OnCancel);
        }

        private void Start()
        {
            _list.gameObject.SetActive(false);
        }

        public void ShowMenu(Vector3 screenPosition, IEnumerable<MenuItem> menuItems, Action onCancel = null)
        {
            _onCancel = onCancel;
            _list.gameObject.SetActive(true);
            Clear();
            _items.AddRange(menuItems);

            if (_items.Count == 0)
            {
                Hide();
                return;
            }

            foreach (var item in _items)
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

        private void OnCancel()
        {
            Hide();
            _onCancel?.Invoke();
        }

        private void Clear()
        {
            _items.Clear();

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
