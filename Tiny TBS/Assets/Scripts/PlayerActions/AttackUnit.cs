using Assets.Scripts.Units;

namespace Assets.Scripts.PlayerAction
{
    public struct AttackUnit : IPlayerAction
    {
        public Unit Attacker { get; set; }

        public Unit Defender { get; set; }
    }
}