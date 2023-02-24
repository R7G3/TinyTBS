using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Configs
{
    [CreateAssetMenu]
    public class FractionsConfig : ScriptableObject
    {
        public FractionConfig[] fractions;
    }

    [Serializable]
    public struct FractionConfig
    {
        public string id;
        public Color color;
    }
}
