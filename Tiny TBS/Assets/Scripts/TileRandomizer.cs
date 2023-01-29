using UnityEngine;

public class TileRandomizer : MonoBehaviour
{
    public GameObject[] tilePrefabs;

    void Start()
    {
        var rndObj = tilePrefabs[Random.Range(0, tilePrefabs.Length - 1)];
        var obj = Instantiate(rndObj, transform, false);
        obj.transform.rotation = Quaternion.AngleAxis(Random.Range(0, 4) * 45f, Vector3.up);
    }
}