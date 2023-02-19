using Assets.Scripts.Configs;
using Assets.Scripts.Tiles;
using Assets.Scripts.Units;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class Map
    {
        private BalanceConfig _balanceConfig;
        private ITile[,] _tiles;

        public Map(ITile[,] tiles, BalanceConfig balanceConfig)
        {
            _tiles = tiles;
            _balanceConfig = balanceConfig;
        }

        public Map(int x, int y, BalanceConfig balanceConfig)
        {
            _tiles = new TileView[x, y];
            _balanceConfig = balanceConfig;
        }

        public bool IsValidCoord(Vector2Int coord)
            => coord.x >= 0 && coord.x < _tiles.GetLength(0) &&
               coord.y >= 0 && coord.y < _tiles.GetLength(1);

        public ITile this[int x, int y]
        {
            get { return _tiles[x, y]; }
            set { _tiles[x, y] = value; }
        }

        public ITile this[Vector2Int coord]
        {
            get { return _tiles[coord.x, coord.y]; }
            set { _tiles[coord.x, coord.y] = value; }
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
                        GetNeighborsForPossibleMoves(potentialMove, unit.Speed + 1)
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
            var tile = this[coord];

            if (tile.Building != null || tile.Unit != null)
            {
                return new MoveCost(coord, 100);
            }

            var penalty = _balanceConfig.GetPenaltyFor(tile.Type);
            var cost = previousMovesCost + penalty;

            Debug.Log($"{coord} cost: {cost}");

            return new MoveCost(coord, cost);
        }

        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int coord)
        {
            var valueWithOffset = Normalize(coord.x - 1, 0);

            if (valueWithOffset != coord.x)
            {
                yield return new Vector2Int(valueWithOffset, coord.y);
            }

            valueWithOffset = Normalize(coord.x + 1, 0);

            if (valueWithOffset != coord.x)
            {
                yield return new Vector2Int(valueWithOffset, coord.y);
            }

            valueWithOffset = Normalize(coord.y - 1, 0);

            if (valueWithOffset != coord.y)
            {
                yield return new Vector2Int(coord.x, valueWithOffset);
            }

            valueWithOffset = Normalize(coord.y + 1, 0);

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

            if (value > _tiles.GetLength(axis))
            {
                return _tiles.GetLength(axis);
            }

            return value;
        }
    }
}