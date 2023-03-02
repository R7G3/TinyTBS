using Assets.Scripts.Configs;
using UnityEngine;
using Utils;

namespace Assets.Scripts.Tiles
{
    public static class TileInformation
    {
        public static string GetTileInfo(Vector2Int coord, Map map, BalanceConfig balanceConfig, InfoType type)
        {
            var _coord = coord;

            if (map.IsValidCoord(_coord))
            {
                var tile = map[_coord];

                switch (type)
                {
                    case InfoType.Tile:
                        return GetTileInformationOnMap(tile, balanceConfig);

                    case InfoType.Unit:
                        return GetUnitInformationOnMap(tile);

                    case InfoType.Building:
                        return GetBuildingInformationOnMap(tile);
                    default:
                        return string.Empty;
                }
            }

            return string.Empty;
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
}
