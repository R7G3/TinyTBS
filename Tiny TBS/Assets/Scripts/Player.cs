namespace Assets.Scripts
{
    public class Player
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
    }
}
