using System;
using Unity.VisualScripting;

namespace Assets.Scripts
{
    public class Player : IEquatable<Player>
    {
        private int _gold;

        public Player(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }

        public string Name { get; }

        public int Gold { get; }

        public void AddGold(int amount)
        {
            _gold += amount;
        }

        public bool TryWithdrawGold(int amount)
        {
            if (_gold < amount)
            {
                return false;
            }

            _gold -= amount;
            return true;
        }

        public static bool operator ==(Player left, Player right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Player left, Player right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is Player))
            {
                return false;
            }

            return Equals((Player) obj);
        }

        public bool Equals(Player other)
        {
            return this.Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
