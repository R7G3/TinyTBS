using Assets.Scripts;
using Assets.Scripts.Configs;
using Assets.Scripts.Controllers;
using Assets.Scripts.Tiles;
using Assets.Scripts.Utils;
using UnityEngine;

public class TileInformationController : MonoBehaviour, IService
{
    [SerializeField] private RectTransform _infoWidgetsRoot; 
    [SerializeField] private ServiceLocator _serviceLocator;
    [SerializeField] private GameObject _infoWidgetPrefab;
    
    private TileInfoView _unitInfo;
    private TileInfoView _buildingInfo;
    private TileInfoView _terrainInfo;
    private BalanceConfig _balanceConfig;
    private UnityEngine.UI.LayoutGroup _layout;
    
    private Map _map;

    private void Awake()
    {
        // _layout = gameObject.GetComponent<UnityEngine.UI.LayoutGroup>();
        // _layout.enabled = false;
        _unitInfo = CreateInfo();
        _buildingInfo = CreateInfo();
        _terrainInfo = CreateInfo();
    }

    private void Start()
    {
        _map = _serviceLocator.GetService<Map>();
        _balanceConfig = _serviceLocator.GetService<BalanceConfig>();
    }

    private TileInfoView CreateInfo() =>
        Instantiate(_infoWidgetPrefab, _infoWidgetsRoot, false)
            .GetComponent<TileInfoView>();

    public void Hide()
    {
        _unitInfo.gameObject.SetActive(false);
        _buildingInfo.gameObject.SetActive(false);
        _terrainInfo.gameObject.SetActive(false);
    }

    public void ShowInfoFor(Vector2Int coord)
    {
        var tile = _map[coord];

        _terrainInfo.gameObject.SetActive(true);
        _unitInfo.gameObject.SetActive(tile.Unit != null);
        _buildingInfo.gameObject.SetActive(tile.Building != null);
        
        _terrainInfo.SetText(GetTerrainInfo(tile));
        _buildingInfo.SetText(GetBuildingInfo(tile));
        _unitInfo.SetText(GetUnitInfo(tile));

        // _layout.enabled = true;
        Canvas.ForceUpdateCanvases();
    }
    
    private string GetTerrainInfo(ITile tile)
    {
        string info = "";

        var _tileType = tile.Type.ToString();
        var _tileDefence = _balanceConfig.GetDefenceImpact(tile.Type).ToString();

        info = $"Terrain: {_tileType} \nDefence bonus: {_tileDefence}\n";

        return info;
    }

    private string GetUnitInfo(ITile tile)
    {
        string info = "";

        if (tile.Unit != null)
        {
            var _unitType = tile.Unit.Type.ToString();
            var _unitAttack = tile.Unit.Attack.ToString();
            var _unitHealth = tile.Unit.Health.ToString();
            var _unitDefence = tile.Unit.Defence.ToString();
            var _unitAttackRange = tile.Unit.AttackRange.ToString();
            var _unitSpeed = tile.Unit.Speed.ToString();
            var _unitFraction = tile.Unit.Owner.ToString();

            info += $"Unit: {_unitType} \nAttack: {_unitAttack}\n" +
                $"Health: {_unitHealth}\nDefence: {_unitDefence}\n" +
                $"Attack Range: {_unitAttackRange}\nSpeed: {_unitSpeed}\n " +
                $"Fraction: {_unitFraction}\n";
        }

        return info;
    }

    private static string GetBuildingInfo(ITile tile)
    {
        string info = "";

        if (tile.Building != null)
        {
            var _buildingType = tile.Building.Type.ToString();
            var _buildingState = tile.Building.State.ToString();
            var _buildingBelongsToPlayer = tile.Building.Owner.ToString();

            info += $"Building: {_buildingType} \nState: {_buildingState}\n" +
                $"Fraction: {_buildingBelongsToPlayer}";
        }

        return info;
    }
}

