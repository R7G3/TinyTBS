using UnityEngine;

namespace Assets.Scripts.Tiles
{
    public class TileRandomizer : MonoBehaviour
    {
        public GameObject[] tilePrefabs;

        void Start()
        {
            var rndObj = tilePrefabs[Random.Range(0, tilePrefabs.Length)];
            var obj = Instantiate(rndObj, transform, false);
            obj.transform.rotation = Quaternion.AngleAxis(Random.Range(0, 4) * 90f, Vector3.up);
        }
    }
}