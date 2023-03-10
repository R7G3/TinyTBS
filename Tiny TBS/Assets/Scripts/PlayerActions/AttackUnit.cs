using Assets.Scripts.Units;
using UnityEngine;

namespace Assets.Scripts.PlayerAction
{
    public struct AttackUnit : IPlayerAction
    {
        public Vector2Int StandingCoord { get; set; }
        
        public Unit Attacker { get; set; }

        public Unit Defender { get; set; }
    }
}