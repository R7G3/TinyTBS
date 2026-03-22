using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.GameLogic.Models
{
    public class PathPart
    {
        public PathPart(MoveInfo current)
        {
            CurrentMoveInfo = current;
        }

        public PathPart(PathPart previous, MoveInfo current)
        {
            CurrentMoveInfo = current;
            Previous = previous;
        }

        public MoveInfo CurrentMoveInfo { get; set; }

        public PathPart Previous { get; set; }

        public PathPart Next { get; set; }

        public IEnumerable<Vector2Int> GetTrackToHead()
        {
            var node = this;
            MoveInfo step = null;

            while (node.Previous != null)
            {
                step = node.Previous.CurrentMoveInfo;

                if (step.CanMove)
                {
                    yield return step.Coord;
                }

                node = node.Previous;
            }
        }
    }
}
