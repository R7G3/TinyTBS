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

            yield return node.CurrentMoveInfo.Coord;

            while (node.Previous != null)
            {
                step = node.Previous.CurrentMoveInfo;

                if (!step.CanMove)
                {
                    node = node.Previous;

                    continue;
                }

                if (IsDiagonalOffset(node.CurrentMoveInfo.Coord, step.Coord))
                {
                    yield return step.Coord;
                }

                node = node.Previous;
            }

            yield return node.CurrentMoveInfo.Coord;
        }

        private bool IsDiagonalOffset(Vector2Int previous, Vector2Int following)
        {
            return previous.x != following.x && previous.y != following.y;
        }
    }
}
