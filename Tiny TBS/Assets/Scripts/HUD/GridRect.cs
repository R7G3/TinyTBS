using System;
using Assets.Scripts.Configs;
using UnityEngine;

namespace Assets.Scripts.HUD
{
    public class GridRect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        [SerializeField] private GridDrawerConfig _config;

        public void SetType(GridType type)
        {
            switch (type)
            {
                case GridType.Default:
                    _renderer.color = _config.defaultColor;
                    break;
                case GridType.AvailableForAttack:
                    _renderer.color = _config.availableForAttackColor;
                    break;
                case GridType.Enemy:
                    _renderer.color = _config.enemyColor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}