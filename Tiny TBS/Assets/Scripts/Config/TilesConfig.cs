using System;
using UnityEngine;

namespace Assets.Scripts.Config
{
    [CreateAssetMenu]
    public class TilesConfig : ScriptableObject
    {

        public TilePrefabs tilePrefabs = new();
        
        [Serializable]
        public class TilePrefabs
        {
            public GameObject grass;
            public GameObject road;
            public GameObject mountain;
            public GameObject water;
        }
    }
}