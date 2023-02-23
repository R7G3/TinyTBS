using Assets.Scripts.Configs;
using Assets.Scripts.Units;
using System;
using UnityEngine;

namespace Assets.Scripts.GameLogic
{
    public class Attack
    {
        private readonly BalanceConfig _balanceConfig;

        public Attack(BalanceConfig balanceConfig)
        {
            _balanceConfig = balanceConfig;
        }

        public void Calculate(Unit atacker, Unit defender)
        {
            //
        }
    }
}