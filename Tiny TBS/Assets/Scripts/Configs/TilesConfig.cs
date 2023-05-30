using System;
using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.Configs
{
    [CreateAssetMenu]
    public class TilesConfig : ScriptableObject, IService
    {

        public TilePrefabs tilePrefabs = new();
        public BuildingPrefabs buildingPrefabs = new();
        
        [Serializable]
        public class BuildingPrefabs
        {
            public GameObject village;
            public GameObject castle;
        }
        
        [Serializable]
        public class TilePrefabs
        {
            public GameObject grass;
            public GameObject road;
            public GameObject mountain;
            public GameObject water;
        }

        public static TilesConfig instance => Resources.Load<TilesConfig>("Config/TilesConfig");
    }
}
