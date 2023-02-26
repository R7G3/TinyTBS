using Assets.Scripts;
using Assets.Scripts.Buildings;
using Assets.Scripts.Configs;
using Assets.Scripts.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class TileInformation : MonoBehaviour
{
    private readonly Map _map;
    private readonly BalanceConfig _balanceConfig;
    private readonly Camera _camera;

    private string _tileType;
    private string _tileSpeedImpact;
    private string _tileDefence;

    private string _buildingType;
    private string _buildingState;
    private string _buildingBelongsToPlayer;

    private string _unitType;
    private string _unitAttack;
    private string _unitHealth;
    private string _unitDefence;
    private string _unitAttackRange;
    private string _unitSpeed;
    private string _unitFraction;

    public TileInformation(Map map, BalanceConfig balanceConfig, Camera camera)
    {
        _map = map;
        _balanceConfig = balanceConfig;
        _camera = camera;
    }

    public string GetTileInfo(Vector3 pos)
    {
        var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);

        if (_map.IsValidCoord(coord))
        {
            var tile = _map[coord];

            _tileType = tile.Type.ToString();
            _tileDefence = _balanceConfig.GetDefenceImpact(tile.Type).ToString();

            if (tile.Unit != null)
            {
                _unitType = tile.Unit.Type.ToString();
                _unitAttack = tile.Unit.Attack.ToString();
                _unitHealth = tile.Unit.Health.ToString();
                _unitDefence = tile.Unit.Defence.ToString();
                _unitAttackRange = tile.Unit.AttackRange.ToString();
                _unitSpeed = tile.Unit.Speed.ToString();
                _unitFraction = tile.Unit.Fraction.ToString();

                _tileSpeedImpact = _balanceConfig.GetPenaltyFor(tile.Type, tile.Unit).ToString();
            }
            else
            {
                _tileSpeedImpact = "test";
            }

            if (tile.Building != null)
            {
                _buildingType = tile.Building.Type.ToString();
                _buildingState = tile.Building.State.ToString();
                _buildingBelongsToPlayer = tile.Building.Fraction.ToString();
            }
        }

        string text = $"Terrain: {_tileType} \nDefence bonus: {_tileDefence}";
        return text;
    }
}