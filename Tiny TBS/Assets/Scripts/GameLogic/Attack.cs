using Assets.Scripts.Configs;
using Assets.Scripts.Units;
using System;

namespace Assets.Scripts.GameLogic
{
    public class Attack
    {
        private readonly Map _map;
        private readonly BalanceConfig _balanceConfig;

        public Attack(Map map , BalanceConfig balanceConfig)
        {
            _map = map;
            _balanceConfig = balanceConfig;
        }

        public int CalculateDamage(Unit attacker, Unit defender)
        {
            var attakerDamage = CalculateAttack(attacker);
            var defenderDefence = CalculateDefence(defender);

            int totalDamage = attakerDamage - defenderDefence;

            return totalDamage >= 0 ? totalDamage : 0;
        }

        private int CalculateAttack(Unit attacker)
        {
            var attackPercent = attacker.Attack / 100d;
            var healthPercent = 100d * ((double)attacker.Health / Definitions.MaxUnitHealth);
            var totalAttack = attackPercent * healthPercent + _balanceConfig.attackImpact;
            
            var damage  = (int)Math.Round(totalAttack);

            return damage;
        }

        private int CalculateDefence(Unit defender)
        {
            var tileDefence = defender.IsFlying ? 0 : _balanceConfig.GetDefenceImpact(_map[defender.Coord].Type);
            var villageDefence = defender.IsInVillage ? _balanceConfig.villageDefenceBonus : 0;
            var sumDefece = (double)defender.Defence + tileDefence + villageDefence + _balanceConfig.defenceImpact;
            var healthPercent = (double)defender.Health / Definitions.MaxUnitHealth;

            var defence = (int)Math.Round(healthPercent * sumDefece);

            return defence;
        }
    }
}