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
    }
}
