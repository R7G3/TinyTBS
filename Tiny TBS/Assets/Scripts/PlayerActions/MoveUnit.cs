using Assets.Scripts.Units;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.PlayerAction
{
    public struct MoveUnit : IPlayerAction
    {
        public Unit unit;

        public Vector2Int coord;

        public IEnumerable<Vector2Int> track;
    }
}