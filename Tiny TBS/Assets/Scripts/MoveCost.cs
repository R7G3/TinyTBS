using System;
using UnityEngine;

namespace Assets.Scripts
{
    public class MoveCost : IEquatable<MoveCost>
    {
        public MoveCost(Vector2Int coord, int cost)
        {
            Coord = coord;
            Cost = cost;
        }

        public Vector2Int Coord { get; }

        public int Cost { get; }

        public override bool Equals(object obj)
        {
            return obj is MoveCost cost &&
                   Coord.Equals(cost.Coord);
        }

        public bool Equals(MoveCost other)
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
