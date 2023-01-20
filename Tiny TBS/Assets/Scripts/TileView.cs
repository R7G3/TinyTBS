using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileView : MonoBehaviour
{
    private Tile _tile;
    private MeshRenderer _renderer;

    public void Init(Tile tile)
    {
        _tile = tile;

        switch (tile.Type)
        {
            case TileType.Road:
                _renderer.material.color = Color.black;
                break;

            case TileType.Grass:
                _renderer.material.color = Color.green;
                break;

            case TileType.Mountain:
                _renderer.material.color = Color.gray;
                break;

            case TileType.Water:
                _renderer.material.color = Color.blue;
                break;

            default:
                _renderer.material.color = Color.cyan;
                break;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
    }

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
