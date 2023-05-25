using Assets.Scripts;
using Assets.Scripts.Tiles;
using Assets.Scripts.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TileInformationVisibilityController : MonoBehaviour, IService
{
    [SerializeField] private GameObject _terrainInfoWidget;
    [SerializeField] private GameObject _unitInfoWidget;
    [SerializeField] private GameObject _buildingInfoWidget;

    public void ChangeVisibility(Vector2Int coord, Map map)
    {
        var tile = map[coord];

        if(tile.Unit != null)
        {
            _unitInfoWidget.SetActive(true);
        }
        else
        {
            _unitInfoWidget.SetActive(false);
        }

        if(tile.Building != null)
        {
            _buildingInfoWidget.SetActive(true);
        }
        else
        {
            _buildingInfoWidget.SetActive(false);
        }
    }
}

