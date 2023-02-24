using Assets.Scripts.Buildings;
using Assets.Scripts.Configs;
using Assets.Scripts.GameLogic.Models;
using Assets.Scripts.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.GameLogic
{
    public class MapActions
    {
        private readonly Map _map;
        private readonly BalanceConfig _balanceConfig;

        public MapActions(Map map, BalanceConfig balanceConfig)
        {
            _map = map;
            _balanceConfig = balanceConfig;
        }

        public bool IsNeighbors(Vector2Int a, Vector2Int b)
        {
            var difference = a - b;
            var x = Math.Abs(difference.x);

            if (x == 0 || x == 1)
            {
                var y = Math.Abs(difference.y);

                return y == 0 || y == 1;
            }

            return false;
        }

        public IEnumerable<MoveCost> GetPossibleMoves(Unit unit)
        {
            var processed = new HashSet<MoveCost>();
            var result = new List<MoveCost>();
            var startPoint = new MoveCost(unit.Coord, 0);

            var movesForProcessing = new List<MoveCost>(
                GetNeighboursForPossibleMoves(startPoint, unit.Speed));

            while (movesForProcessing.Any())
            {
                result.AddRange(movesForProcessing);
                var forChecking = new List<MoveCost>();

                foreach (var potentialMove in movesForProcessing)
                {
                    processed.Add(potentialMove);

                    forChecking.AddRange(
                        GetNeighboursForPossibleMoves(potentialMove, unit.Speed)
                            .Where(x => !processed.Contains(x)));
                }

                movesForProcessing.Clear();
                movesForProcessing.AddRange(forChecking);
            }

            return result
                .Distinct();
        }

        public IEnumerable<Vector2Int> GetTrackForMoving(Vector2Int start, Vector2Int finish, MoveCost[] moveCosts)
        {
            IEnumerable<Vector2Int> neighbours;
            IEnumerable<MoveCost> neighbourCosts;
            int minimalCost = 0;

            var previousMove = moveCosts.Single(move => move.Coord == finish);
            var result = new List<Vector2Int>();
            var didNotCome = true;
            result.Add(previousMove.Coord);

            while (didNotCome)
            {
                neighbours = GetNeighbours(previousMove.Coord);
                neighbourCosts = moveCosts.Where(move => neighbours.Contains(move.Coord));
                minimalCost = neighbourCosts.Min(move => move.Cost);
                
                previousMove = neighbourCosts.Single(move => move.Cost == minimalCost);

                if (previousMove.Coord == start)
                {
                    didNotCome = false;
                    continue;
                }

                result.Add(previousMove.Coord);
            }

            return result;
        }

        public IEnumerable<Vector2Int> GetPossibleTargetsForAttack(Unit unit)
        {
            var position = new MoveCost(unit.Coord, 0);

            var coordsInRange = GetNeighboursForPossibleMoves(position, unit.AttackRange);

            foreach (var coord in coordsInRange.Select(c => c.Coord))
            {
                if (CoordinateInRange(unit, coord)
                    && HasEnemyUnit(unit, coord))
                {
                    yield return coord;
                }
            }
        }

        public IEnumerable<Vector2Int> GetPossibleTargetsForOccupy(Unit unit)
        {
            return GetNeighbours(unit.Coord)
                .Where(neighbourn => HaveOccupableBuilding(unit, neighbourn));
        }
        
        public bool HasEnemyUnit(Unit unit, Vector2Int coord)
        {
            var otherUnit = _map[coord].Unit;

            if (otherUnit == null)
            {
                return false;
            }

            if (otherUnit.Fraction.Id.Equals(unit.Fraction.Id, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private bool CoordinateInRange(Unit unit, Vector2Int coord)
        {
            var rangeByX = Math.Abs(coord.x - unit.Coord.x);
            var rangeByY = Math.Abs(coord.y - unit.Coord.y);
            var summaryRange = rangeByX + rangeByY;

            return summaryRange <= unit.AttackRange;
        }


        private bool HaveOccupableBuilding(Unit unit, Vector2Int coord)
        {
            var building = _map[coord].Building;

            if (building == null)
            {
                return false;
            }

            if (building.State == BuildingState.Broken)
            {
                return false;
            }

            if (building.Fraction.Id.Equals(unit.Fraction.Id, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // TODO: ���������, ����� �� ���� �����������
            return true;
        }

        private IEnumerable<MoveCost> GetNeighboursForPossibleMoves(MoveCost previousMove, int unitSpeed)
        {
            var result = new List<MoveCost>(4);
            var neighbours = GetNeighbours(previousMove.Coord);

            foreach (var neighbour in neighbours)
            {
                var potentialMovement = GetCostForMove(neighbour, previousMove.Cost);

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

        public IEnumerable<Vector2Int> GetNeighbours(Vector2Int coord)
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
