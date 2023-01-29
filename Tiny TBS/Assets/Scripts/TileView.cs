using Assets.Scripts;
using UnityEngine;

public class TileView : MonoBehaviour
{
    private MeshRenderer _renderer;

    public Color roadColor;
    public Color mountainColor;
    public Color grassColor;
    public Color waterColor;
    public Color unknown;

    public GameObject castlePrefab;

    public void SetType(TileType tileType)
    {
        return;
        switch (tileType)
        {
            case TileType.Road:
                _renderer.material.color = roadColor;
                break;

            case TileType.Grass:
                _renderer.material.color = grassColor;
                break;

            case TileType.Mountain:
                _renderer.material.color = mountainColor;
                break;

            case TileType.Water:
                _renderer.material.color = waterColor;
                break;

            default:
                _renderer.material.color = unknown;
                break;
        }
    }

    public void SetBuilding(Building building)
    {
        switch (building.Type)
        {
            case BuildingType.None:
                break;

            case BuildingType.Village:
                break;

            case BuildingType.Castle:
                {
                    var castleView = Instantiate(
                        castlePrefab,
                        Vector3.zero,
                        Quaternion.identity);

                    castleView.transform.SetParent(
                        transform,
                        worldPositionStays: false);

                    break;
                };

            default:
                break;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
    }

    void Awake()
    {
        _renderer = GetComponentInChildren<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
