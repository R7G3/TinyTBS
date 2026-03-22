using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.GameLogic.Models
{
    public class MoveInfo : IEquatable<MoveInfo>
    {
        public MoveInfo(
            Vector2Int coord,
            int cost,
            bool canMove,
            bool canAttack,
            bool canOccupy,
            PathPart pathwayPart)
        {
            Coord = coord;
            Cost = cost;
            CanMove = canMove;
            CanAttack = canAttack;
            CanOccupy = canOccupy;
            PathwayPart = pathwayPart;
        }

        public Vector2Int Coord { get; }

        public int Cost { get; }

        public bool CanMove { get; }

        public bool CanAttack { get; }

        public bool CanOccupy { get; }

        public bool HasAvailableActions => CanMove || CanAttack || CanOccupy;

        public PathPart PathwayPart { get; set; }

        public static MoveInfo CreateDefault(Vector2Int coord)
        {
            var moveInfo = new MoveInfo(
                coord: coord,
                cost: 0,
                canMove: false,
                canAttack: false,
                canOccupy: false,
                pathwayPart: null);

            var pathPart = new PathPart(current: moveInfo);

            moveInfo.PathwayPart = pathPart;

            return moveInfo;
        }

        public static MoveInfo CreateWithMaxCost(Vector2Int coord)
        {
            var moveInfo = new MoveInfo(
                coord: coord,
                cost: 100,
                canMove: false,
                canAttack: false,
                canOccupy: false,
                pathwayPart: null);

            var pathPart = new PathPart(current: moveInfo);

            moveInfo.PathwayPart = pathPart;

            return moveInfo;
        }

        public override bool Equals(object obj)
        {
            return obj is MoveInfo moveInfo &&
                   Coord.Equals(moveInfo.Coord);
        }

        public bool Equals(MoveInfo other)
        {
            return other != null &&
                Coord == other.Coord;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Coord);
        }
    }
}
