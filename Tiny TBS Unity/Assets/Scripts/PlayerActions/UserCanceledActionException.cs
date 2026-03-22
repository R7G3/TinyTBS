using Assets.Scripts.Units;
using System;
using UnityEngine;

namespace Assets.Scripts.PlayerAction
{
    public class UserCanceledActionException : Exception//MonoBehaviour
    {
        public Unit unit;

        public Vector2Int coord;
    }
}
