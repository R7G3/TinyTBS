using Assets.Scripts.Units;
using UnityEngine;

namespace Assets.Scripts.PlayerAction
{
    public struct OccupyBuilding : IPlayerAction
    {
        public Unit Unit { get; set; }

        public Vector2Int Coord { get; set; }

        public bool NeedToCome { get; set; }
    }
}
