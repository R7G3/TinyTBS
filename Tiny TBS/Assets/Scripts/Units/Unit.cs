using System;
using UnityEngine;

namespace Assets.Scripts.Units
{
    public class Unit
    {
        public Guid Id { get; } = Guid.NewGuid();

        public UnitType Type { get; set; }

        public IFraction Fraction { get; set; }

        public int Health { get; set; }

        public int Speed { get; set; }

        public int Defence { get; set; }

        public int Attack { get; set; }

        public int AttackRange { get; set; }

        public bool IsFlying { get; set; }

        public bool IsSwimming { get; set; }

        public bool HasMoved { get; set; }
        
        public bool HasPerformedAction { get; set; }

        public bool IsInVillage { get; set; }

        public Vector2Int Coord { get; set; }
    }
}