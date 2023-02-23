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
            var defenderDefece = CalculateDefence(defender);

            int totalDamage = attakerDamage - defenderDefece;

            return totalDamage;
        }

        private int CalculateAttack(Unit attacker)
        {
            int attakerDamage;

            var attackPercent = attacker.Attack / 100;
            var healthPercent = 100 * (attacker.Health / Definitions.MaxUnitHealth);
            var totalAttack = attackPercent * healthPercent * _balanceConfig.attackCoefficient;
            
            attakerDamage = (int)Math.Round(totalAttack);

            return attakerDamage;
        }

        private int CalculateDefence(Unit defender)
        {
            int defenderDefece;

            var villageDefence = defender.IsInVillage ? _balanceConfig.villageDefenceBonus : 0;
            var tileDefence = _balanceConfig.GetDefenceBonusFor(_map[defender.Coord].Type);
            var sumDefece = (double)defender.Defence + tileDefence + villageDefence;
            var healthPercent = defender.Health / Definitions.MaxUnitHealth;

            defenderDefece = (int)Math.Round(healthPercent * sumDefece);

            return defenderDefece;
        }

        private bool IsNeighborsCells(Unit attacker, Unit defender)
        {
            return true;
        }
    }
}