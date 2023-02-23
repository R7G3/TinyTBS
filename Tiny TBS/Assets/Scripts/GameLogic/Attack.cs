using Assets.Scripts.Configs;
using Assets.Scripts.Units;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

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

        private int CalculateDamage(Unit attacker, Unit defender)
        {
            var attakerDamage = CalculateAttack(attacker);
            var defenderDefence = CalculateDefence(defender);

            int totalDamage = attakerDamage - defenderDefence;

            return totalDamage;
        }

        private int CalculateAttack(Unit attacker)
        {
            int attakerDamage;

            var attackPercent = attacker.Attack / 100;
            var healthPercent = 100 * (attacker.Health / Definitions.MaxUnitHealth);
            var totalAttack = attackPercent * healthPercent + _balanceConfig.attackCoefficient;
            
            attakerDamage = (int)Math.Round((double)totalAttack);

            return attakerDamage;
        }

        private int CalculateDefence(Unit defender)
        {
            int defenderDefece;

            var tileDefence = defender.IsFlying ? 0 : _balanceConfig.GetDefenceImpact(_map[defender.Coord].Type);
            var villageDefence = defender.IsInVillage ? _balanceConfig.villageDefenceBonus : 0;
            var sumDefece = (double)defender.Defence + tileDefence + villageDefence;
            var healthPercent = defender.Health / Definitions.MaxUnitHealth;

            defenderDefece = (int)Math.Round(healthPercent * sumDefece);

            return defenderDefece;
        }
    }
}