using Assets.Scripts;
using Assets.Scripts.Buildings;
using Assets.Scripts.Configs;
using Assets.Scripts.Tiles;
using Assets.Scripts.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utils;

public static class TileInformation
{
    public static string GetTileInfo(Vector3 pos, Map map, BalanceConfig balanceConfig, Camera camera, InfoType type)
    {
        var _map = map;
        var _camera = camera;
        var _balanceConfig = balanceConfig;

        var coord = FieldUtils.GetCoordFromMousePos(pos, _camera);
        
        string text = "";

        if (_map.IsValidCoord(coord))
        {
            var tile = _map[coord];

            var _tileType = tile.Type.ToString();
            var _tileDefence = _balanceConfig.GetDefenceImpact(tile.Type).ToString();

            text = $"Terrain: {_tileType} \nDefence bonus: {_tileDefence}\n";

            switch (type)
            {
                case InfoType.Tile:
                    return GetTileInformationOnMap(tile, _balanceConfig);

                case InfoType.Unit:
                    return GetUnitInformationOnMap(tile);

                case InfoType.Building:
                    return GetBuildingInformationOnMap(tile);
                default:
                    return text;
            }
        }

        return text;
    }

    private static string GetTileInformationOnMap(ITile tile, BalanceConfig _balanceConfig)
    {
        string info = "";

        var _tileType = tile.Type.ToString();
        var _tileDefence = _balanceConfig.GetDefenceImpact(tile.Type).ToString();

        info = $"Terrain: {_tileType} \nDefence bonus: {_tileDefence}\n";

        return info;
    }

    private static string GetUnitInformationOnMap(ITile tile)
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
            var _unitFraction = tile.Unit.Fraction.ToString();

            info += $"\nUnit: {_unitType} \nAttack: {_unitAttack}\n" +
                $"Health: {_unitHealth}\nDefence: {_unitDefence}\n" +
                $"Attack Range: {_unitAttackRange}\nSpeed: {_unitSpeed}\n " +
                $"Fraction: {_unitFraction}\n";
        }

        return info;
    }

    private static string GetBuildingInformationOnMap(ITile tile)
    {
        string info = "";

        if (tile.Building != null)
        {
            var _buildingType = tile.Building.Type.ToString();
            var _buildingState = tile.Building.State.ToString();
            var _buildingBelongsToPlayer = tile.Building.Fraction.ToString();

            info += $"\nBuilding: {_buildingType} \nState: {_buildingState}\n" +
                $"Fraction: {_buildingBelongsToPlayer}";
        }

        return info;
    }
}