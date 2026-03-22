using UnityEngine;

namespace Assets.Scripts.Configs
{
    [CreateAssetMenu]
    public class GridDrawerConfig : ScriptableObject
    {
        public Color defaultColor;
        public Color mouseOverColor;
        public Color selectedColor;
        public Color availableForAttackColor;
        public Color enemyColor;
    }
}
