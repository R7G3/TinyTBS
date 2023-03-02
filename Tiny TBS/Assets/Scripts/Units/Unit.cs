using System;
using UnityEngine;

namespace Assets.Scripts.Units
{
    public class Unit
    {
        public Unit(
            UnitType unitType,
            IFraction fraction,
            int speed,
            int defence,
            int attack,
            int attackRange,
            bool isFlying,
            bool isSwimming,
            Vector2Int coord
            )
        {
            Type = unitType;
            Fraction = fraction;
            Health = Definitions.MaxUnitHealth;
            Speed = speed;
            Defence = defence;
            Attack = attack;
            AttackRange = attackRange;
            IsFlying = isFlying;
            IsSwimming = isSwimming;
            Coord = coord;
        }

        public Unit(IFraction fraction, Vector2Int coord)
        {
            Type = UnitType.Soldier;
            Fraction = fraction;
            Health = Definitions.MaxUnitHealth;
            Speed = 3;
            Defence = 3;
            Attack = 5;
            AttackRange = 1;
            IsFlying = false;
            IsSwimming = false;
            Coord = coord;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public UnitType Type { get; set; }

        public IFraction Fraction { get; set; }

        public int Health { get; set; }

        public int Speed { get; set; }

        public int Defence { get; set; }

        public int Attack { get; set; }

        public int AttackRange { get; set; } = 1;

        public bool IsFlying { get; set; }

        public bool IsSwimming { get; set; }

        public bool HasMoved { get; set; }
        
        public bool HasPerformedAction { get; set; }

        public bool IsInVillage { get; set; }

        public Vector2Int Coord { get; set; }
    }
}