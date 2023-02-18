using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.HUD.Menu
{
    public class MenuItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private Button _button;
        [SerializeField] private RectTransform _hoverIndicator;

        private void Awake()
        {
            _hoverIndicator.gameObject.SetActive(false);
        }

        public void Init(string title, Action onClick)
        {
            _title.text = title;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(onClick.Invoke);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hoverIndicator.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hoverIndicator.gameObject.SetActive(false);
        }
    }
}