using Assets.Scripts.Units;
using UnityEngine;

namespace Assets.Scripts.PlayerAction
{
    public struct MoveUnit : IPlayerAction
    {
        public Unit unit;

        public Vector2Int coord;
    }
}