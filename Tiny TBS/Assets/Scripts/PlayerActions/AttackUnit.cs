using Assets.Scripts.Units;

namespace Assets.Scripts.PlayerAction
{
    public struct AttackUnit : IPlayerAction
    {
        public Unit Attacking { get; set; }

        public Unit Attacked { get; set; }

        public bool NeedToCome { get; set; }
    }
}