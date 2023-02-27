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
    [SerializeField] private TextMeshProUGUI _tileInfo;

    public void SetTileInfo(string text)
    {
        _tileInfo.text = text;
    }
}
