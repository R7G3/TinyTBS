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

        public IEnumerable<Vector2Int> GetTrackForMoving(Vector2Int start, Vector2Int finish, MoveInfo[] moveCosts)
        {
            IEnumerable<Vector2Int> neighbours;
            IEnumerable<MoveInfo> neighbourCosts;
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
            var coordsInRange = GetNeighboursInRange(unit.Coord, unit.AttackRange);

            foreach (var coord in coordsInRange)
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

            if (otherUnit.Fraction.Id == unit.Fraction.Id)
            {
                return false;
            }

            return true;
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

        public IEnumerable<Vector2Int> GetNeighboursInRange(Vector2Int coord, int range)
        {
            throw new NotImplementedException(nameof(GetNeighboursInRange));
        }

        public IEnumerable<MoveInfo> GetPossibleActions(Unit unit)
        {
            var result = new List<MoveInfo>();
            var startPoint = MoveInfo.CreateDefault(unit.Coord);
            var processed = new HashSet<MoveInfo>();
            processed.Add(startPoint);

            var availableMoves = new List<MoveInfo>(
                GetNeighboursForPossibleActions(startPoint, unit));

            availableMoves.Remove(startPoint);

            result.AddRange(availableMoves);

            var forChecking = new List<MoveInfo>();
            var possibles = new List<MoveInfo>(result);
            var returnedCount = availableMoves.Count();

            while (returnedCount != 0)
            {
                var newPossibles = new List<MoveInfo>();
                result.ForEach(possible => 
                    newPossibles.AddRange(GetNeighboursForPossibleActions(possible, unit)
                        .Where(possible => !processed.Contains(possible))));

                possibles = newPossibles;

                returnedCount = possibles.Count();

                foreach (var possible in possibles)
                {
                    if (!processed.Contains(possible))
                    {
                        processed.Add(possible);
                    }

                    if (!result.Contains(possible))
                    {
                        result.Add(possible);
                    }
                }
            }

            return result.Where(cell => cell.CanMove || cell.CanAttack || cell.CanOccupy)
                .Where(cell =>
                {
                    if (cell.CanAttack || cell.CanOccupy)
                    {
                        return cell.PathwayPart.Previous.CurrentMoveInfo.Cost <= unit.Speed;
                    }
                    return true;
                });
        }

        private IEnumerable<MoveInfo> GetNeighboursForPossibleActions(MoveInfo previousMove, Unit unit)
        {
            var result = new List<MoveInfo>(4);
            var neighbours = GetNeighbours(previousMove.Coord);

            foreach (var neighbour in neighbours)
            {
                var potentialMovement = GetMoveInformation(neighbour, previousMove, unit);

                var costOutOfRange = unit.Speed + unit.AttackRange < potentialMovement.Cost;

                if (!costOutOfRange ||
                    potentialMovement.CanAttack ||
                    potentialMovement.CanOccupy)
                {
                    result.Add(potentialMovement);
                }
            }

            return result;
        }

        private MoveInfo GetMoveInformation(Vector2Int coord, MoveInfo previousMove, Unit unit)
        {
            var tile = _map[coord];
            var canAttack = false;
            var canOccupy = false;

            if (tile.Unit != null)
            {
                canAttack = unit.Fraction != tile.Unit.Fraction;
            }

            if (tile.Building != null)
            {
                canOccupy = unit.Fraction != tile.Building.Fraction;
            }

            var penalty = _balanceConfig.GetPenaltyFor(tile.Type, unit);
            var cost = (canAttack || canOccupy) ? 100 : previousMove.Cost + penalty;
            var canMove = cost <= unit.Speed;

            var moveInfo = new MoveInfo(
                    coord: coord,
                    cost: cost,
                    canMove: canMove,
                    canAttack: canAttack,
                    canOccupy: canOccupy,
                    pathwayPart: null);

            var pathPart = new PathPart(
                previous: previousMove.PathwayPart,
                current: moveInfo);

            moveInfo.PathwayPart = pathPart;

            return moveInfo;
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

            if (building.Fraction.Id == unit.Fraction.Id)
            {
                return false;
            }

            // TODO: проверить может ли юнит захватывать
            return true;
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
