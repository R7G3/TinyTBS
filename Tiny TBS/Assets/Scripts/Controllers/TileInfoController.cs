using Assets.Scripts.Configs;
using Assets.Scripts;
using Assets.Scripts.HUD.Menu;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Utils;
using System;
using Assets.Scripts.Controllers;
using Assets.Scripts.HUD;
using Assets.Scripts.Buildings;
using System.Reflection.Emit;

public class TileInfoController : MonoBehaviour
{
    [SerializeField] private RectTransform _list;
    [SerializeField] private TextMeshProUGUI _title;

    private void Start()
    {
        _list.gameObject.SetActive(true);
    }

    public void UpdateHUDInfo(string text)
    {
        _title.text = text;
    }
}
