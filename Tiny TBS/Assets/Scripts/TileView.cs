using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameTile = Assets.Scripts.Tile;

public class TileView : MonoBehaviour
{
    private GameTile _tile;
    private SpriteRenderer _renderer;

    public void Init(GameTile tile)
    {
        _tile = tile;
        _renderer.sprite = Resources.Load<Sprite>("Grass");
    }

    // Start is called before the first frame update
    private void Start()
    {
        Init(
            new GameTile()
            {
                Type = Assets.Scripts.TileType.Grass,
            });
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
