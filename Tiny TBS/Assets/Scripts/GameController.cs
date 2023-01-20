using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    TileView CreateTile(TileType type, Vector3 vector)
    {
        var tileResource = Resources.Load<TileView>("models/tile/Tile");
        var tile = new Tile()
        {
            Type = type,
        };
        var tileView = Instantiate(tileResource, vector, Quaternion.identity);
        tileView.GetComponent<TileView>().Init(tile);

        return tileView;
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateTile(TileType.Grass, new Vector3(0, 0, 0));
        CreateTile(TileType.Road, new Vector3(1, 0, 0));
        CreateTile(TileType.Water, new Vector3(1, 0, 1));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
