using Assets.Scripts.Tiles;
using UnityEngine;

namespace Assets.Scripts.Configs
{
    [CreateAssetMenu]
    public class BalanceConfig : ScriptableObject
    {
        public int roadMovementSpeedPenalty;

        public int grassMovementSpeedPenalty;

        public int mountainMovementSpeedPenalty;

        public int waterMovementSpeedPenalty;

        public int roadDefenceBonus = 0;

        public int grassDefenceBonus = 1;
        
        public int mountainDefenceBonus = 3;
        
        public int waterDefenceBonus = -1;

        public int villageDefenceBonus = 2;

        public int attackCoefficient = 1;

        public int defenceCoefficient = 1;

        public int GetPenaltyFor(TileType type)
        {
            return type switch
            {
                TileType.Road => roadMovementSpeedPenalty,
                TileType.Grass => grassMovementSpeedPenalty,
                TileType.Mountain => mountainMovementSpeedPenalty,
                TileType.Water => waterMovementSpeedPenalty,
                _ => 0,
            };
        }

        public int GetDefenceImpact(TileType type)
        {
            return type switch
            {
                TileType.Road => roadDefenceBonus,
                TileType.Grass => grassDefenceBonus,
                TileType.Mountain => mountainDefenceBonus,
                TileType.Water => waterDefenceBonus,
                _ => 0,
            };
        }
    }
}
