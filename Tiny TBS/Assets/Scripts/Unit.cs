using System;

namespace Assets.Scripts
{
    public class Unit
    {
        public Guid Id { get; set; }

        public UnitType Type { get; set; }

        public IFraction Fraction { get; set; }

        public int Health { get; set; }

        public int Speed { get; set; }

        public int Attack { get; set; }

        public int Defence { get; set; }

        public bool IsFlying { get; set; }

        public bool IsSwimming { get; set; }

        public bool CanUseTurn { get; set; }
    }
}