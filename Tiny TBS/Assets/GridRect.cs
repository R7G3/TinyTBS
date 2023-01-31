using System;
using UnityEngine;

public class GridRect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;

    [SerializeField] private GridDrawerConfig _config;

    private bool _isSelected;
    private bool _isMouseOver;

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        UpdateState();
    }

    public void SetMouseOver(bool isMouseOver)
    {
        _isMouseOver = isMouseOver;
        UpdateState();
    }

    private void UpdateState()
    {
        if (_isSelected)
        {
            _renderer.color = _config.selectedColor;
            return;
        }

        _renderer.color = _isMouseOver ? _config.mouseOverColor : _config.defaultColor;
    }
}