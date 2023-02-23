using Assets.Scripts.Configs;
using Assets.Scripts.GameLogic.Models;
using Assets.Scripts.Units;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.GameLogic
{
    public class Movement
    {
        private readonly Map _map;
        private readonly BalanceConfig _balanceConfig;

        public Movement(Map map, BalanceConfig balanceConfig)
        {
            _map = map;
            _balanceConfig = balanceConfig;
        }

        public IEnumerable<Vector2Int> GetPossibleMoves(Unit unit)
        {
            var processed = new HashSet<MoveCost>();
            var result = new List<MoveCost>();
            var startPoint = new MoveCost(unit.Coord, 0);

            var movesForProcessing = new List<MoveCost>(
                GetNeighborsForPossibleMoves(startPoint, unit.Speed));

            while (movesForProcessing.Any())
            {
                result.AddRange(movesForProcessing);
                var forChecking = new List<MoveCost>();

                foreach (var potentialMove in movesForProcessing)
                {
                    processed.Add(potentialMove);

                    forChecking.AddRange(
                        GetNeighborsForPossibleMoves(potentialMove, unit.Speed)
                            .Where(x => !processed.Contains(x)));
                }

                movesForProcessing.Clear();
                movesForProcessing.AddRange(forChecking);
            }

            return result
                .Select(r => r.Coord)
                .Distinct();
        }

        private IEnumerable<MoveCost> GetNeighborsForPossibleMoves(MoveCost previousMove, int unitSpeed)
        {
            var result = new List<MoveCost>(4);
            var neighbors = GetNeighbors(previousMove.Coord);

            foreach (var neighbor in neighbors)
            {
                var potentialMovement = GetCostForMove(neighbor, previousMove.Cost);

                if (unitSpeed < potentialMovement.Cost)
                {
                    continue;
                }

                result.Add(potentialMovement);
            }

            return result;
        }

        private MoveCost GetCostForMove(Vector2Int coord, int previousMovesCost)
        {
            var tile = _map[coord];

            if (tile.Building != null || tile.Unit != null)
            {
                return new MoveCost(coord, 100);
            }

            var penalty = _balanceConfig.GetPenaltyFor(tile.Type);
            var cost = previousMovesCost + penalty;

            return new MoveCost(coord, cost);
        }

        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int coord)
        {
            var valueWithOffset = Normalize(coord.x - 1, Definitions.xAxis);

            if (valueWithOffset != coord.x)
            {
                yield return new Vector2Int(valueWithOffset, coord.y);
            }

            valueWithOffset = Normalize(coord.x + 1, Definitions.xAxis);

            if (valueWithOffset != coord.x)
            {
                yield return new Vector2Int(valueWithOffset, coord.y);
            }

            valueWithOffset = Normalize(coord.y - 1, Definitions.yAxis);

            if (valueWithOffset != coord.y)
            {
                yield return new Vector2Int(coord.x, valueWithOffset);
            }

            valueWithOffset = Normalize(coord.y + 1, Definitions.yAxis);

            if (valueWithOffset != coord.y)
            {
                yield return new Vector2Int(coord.x, valueWithOffset);
            }
        }

        private int Normalize(int value, int axis)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > _map.GetSizeByAxis(axis))
            {
                return _map.GetSizeByAxis(axis);
            }

            return value;
        }
    }
}
